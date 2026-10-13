using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

public class EnrolmentsController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;

    public EnrolmentsController(RaceDayDbContext db) => _db = db;

    /// <summary>Enters the logged-in Participant into an event by selecting a category.</summary>
    /// <remarks>
    /// No body needed - the participant comes from the session and the event comes from the category.
    /// A participant can only hold one active enrolment per event, and a full category is rejected.
    /// </remarks>
    /// <response code="201">The enrolment, linking participant, event and category.</response>
    /// <response code="400">The event has already taken place.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not a Participant.</response>
    /// <response code="404">Category does not exist.</response>
    /// <response code="409">Already enrolled in this event, or the category is full.</response>
    [HttpPost("api/categories/{categoryId:int}/enrolments")]
    [Authorize(Roles = Roles.Participant)]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EnrolmentResponse>> Enrol(int categoryId)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == categoryId);
        if (category == null) return NotFound(new ErrorResponse("Category not found."));
        if (category.Event.EventDate < DateOnly.FromDateTime(DateTime.Today))
            return BadRequest(new ErrorResponse("This event has already taken place."));

        var me = CurrentUserId;

        var alreadyInEvent = await _db.Enrolments.AnyAsync(e =>
            e.ParticipantID == me &&
            e.Status == EnrolmentStatuses.Confirmed &&
            e.Category.EventID == category.EventID);
        if (alreadyInEvent)
            return Conflict(new ErrorResponse("You are already enrolled in this event."));

        var confirmed = await _db.Enrolments.CountAsync(e =>
            e.CategoryID == categoryId && e.Status == EnrolmentStatuses.Confirmed);
        if (confirmed >= category.MaxParticipants)
            return Conflict(new ErrorResponse("This category is full."));

        // A previously cancelled enrolment for the same category is re-activated (unique per participant+category).
        var enrolment = await _db.Enrolments.FirstOrDefaultAsync(e => e.ParticipantID == me && e.CategoryID == categoryId);
        if (enrolment == null)
        {
            enrolment = new Enrolment { ParticipantID = me, CategoryID = categoryId };
            _db.Enrolments.Add(enrolment);
        }
        else
        {
            enrolment.Status = EnrolmentStatuses.Confirmed;
            enrolment.EnrolmentDate = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();

        var saved = await _db.Enrolments.WithDetails().FirstAsync(e => e.EnrolmentID == enrolment.EnrolmentID);
        return Created("/api/enrolments/me", EnrolmentResponse.From(saved));
    }

    /// <summary>Lists the logged-in Participant's own enrolments (past and upcoming).</summary>
    /// <response code="200">Array of the participant's enrolments, including results where captured.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not a Participant.</response>
    [HttpGet("api/enrolments/me")]
    [Authorize(Roles = Roles.Participant)]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<EnrolmentResponse>>> GetMyEnrolments()
    {
        var me = CurrentUserId;
        var items = await _db.Enrolments.AsNoTracking().WithDetails()
            .Where(e => e.ParticipantID == me)
            .OrderByDescending(e => e.Category.Event.EventDate)
            .ToListAsync();

        return Ok(items.Select(e => EnrolmentResponse.From(e)).ToList());
    }

    /// <summary>Cancels one of the logged-in Participant's own enrolments.</summary>
    /// <response code="204">Enrolment cancelled.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not a Participant, or the enrolment belongs to someone else.</response>
    /// <response code="404">Enrolment does not exist.</response>
    [HttpDelete("api/enrolments/{id:int}")]
    [Authorize(Roles = Roles.Participant)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelEnrolment(int id)
    {
        var enrolment = await _db.Enrolments.FirstOrDefaultAsync(e => e.EnrolmentID == id);
        if (enrolment == null) return NotFound(new ErrorResponse("Enrolment not found."));
        if (enrolment.ParticipantID != CurrentUserId) return Forbidden("You can only cancel your own enrolments.");

        enrolment.Status = EnrolmentStatuses.Cancelled;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
