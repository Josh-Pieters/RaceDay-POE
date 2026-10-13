using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

public class ResultsController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;

    public ResultsController(RaceDayDbContext db) => _db = db;

    /// <summary>Captures a participant's finish time and positions (owning Organiser only).</summary>
    /// <remarks>Body: finishTime (hh:mm:ss), overallPosition, categoryPosition. One result per enrolment.</remarks>
    /// <response code="201">The captured result.</response>
    /// <response code="400">Validation failed, or the enrolment was cancelled.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not an Organiser, or does not own the event.</response>
    /// <response code="404">Enrolment does not exist.</response>
    /// <response code="409">A result has already been captured for this enrolment.</response>
    [HttpPost("api/enrolments/{enrolmentId:int}/results")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResultResponse>> CaptureResult(int enrolmentId, ResultRequest request)
    {
        var enrolment = await _db.Enrolments.WithDetails().FirstOrDefaultAsync(e => e.EnrolmentID == enrolmentId);
        if (enrolment == null) return NotFound(new ErrorResponse("Enrolment not found."));

        var me = CurrentUserId;
        if (enrolment.Category.Event.OrganiserID != me)
            return Forbidden("You can only capture results for your own events.");
        if (enrolment.Status != EnrolmentStatuses.Confirmed)
            return BadRequest(new ErrorResponse("Results can only be captured for confirmed enrolments."));
        if (enrolment.Result != null)
            return Conflict(new ErrorResponse("A result has already been captured for this enrolment."));

        var result = new Result
        {
            Enrolment = enrolment,
            CapturedByOrganiserID = me,
            FinishTime = request.FinishTime,
            OverallPosition = request.OverallPosition,
            CategoryPosition = request.CategoryPosition
        };
        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        return Created($"/api/events/{enrolment.Category.EventID}/results", ResultResponse.From(result));
    }

    /// <summary>Corrects a previously captured result (Organiser who owns the event only).</summary>
    /// <remarks>Body: finishTime (hh:mm:ss), overallPosition, categoryPosition.</remarks>
    /// <response code="200">The updated result.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not an Organiser, or does not own the event.</response>
    /// <response code="404">Result does not exist.</response>
    [HttpPut("api/results/{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultResponse>> UpdateResult(int id, ResultRequest request)
    {
        var result = await _db.Results.WithDetails().FirstOrDefaultAsync(r => r.ResultID == id);
        if (result == null) return NotFound(new ErrorResponse("Result not found."));
        if (result.Enrolment.Category.Event.OrganiserID != CurrentUserId)
            return Forbidden("You can only edit results for your own events.");

        result.FinishTime = request.FinishTime;
        result.OverallPosition = request.OverallPosition;
        result.CategoryPosition = request.CategoryPosition;
        await _db.SaveChangesAsync();

        return ResultResponse.From(result);
    }

    /// <summary>Lists the logged-in Participant's own results across all events.</summary>
    /// <response code="200">Array of the participant's results, newest event first.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not a Participant.</response>
    [HttpGet("api/results/me")]
    [Authorize(Roles = Roles.Participant)]
    [ProducesResponseType(typeof(IEnumerable<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<ResultResponse>>> GetMyResults()
    {
        var me = CurrentUserId;
        var results = await _db.Results.AsNoTracking().WithDetails()
            .Where(r => r.Enrolment.ParticipantID == me)
            .OrderByDescending(r => r.Enrolment.Category.Event.EventDate)
            .ToListAsync();

        return Ok(results.Select(r => ResultResponse.From(r)).ToList());
    }

    /// <summary>Lists all results for an event, ordered by category and position.</summary>
    /// <remarks>Public - no login needed (acts as a leaderboard).</remarks>
    /// <response code="200">Array of results.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpGet("api/events/{id:int}/results")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ResultResponse>>> GetEventResults(int id)
    {
        if (!await _db.Events.AnyAsync(e => e.EventID == id))
            return NotFound(new ErrorResponse("Event not found."));

        var results = await _db.Results.AsNoTracking().WithDetails()
            .Where(r => r.Enrolment.Category.EventID == id)
            .OrderBy(r => r.Enrolment.CategoryID)
            .ThenBy(r => r.CategoryPosition)
            .ToListAsync();

        return Ok(results.Select(r => ResultResponse.From(r)).ToList());
    }
}
