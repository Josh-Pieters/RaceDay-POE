using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[Route("api/events")]
public class EventsController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;

    public EventsController(RaceDayDbContext db) => _db = db;

    /// <summary>Lists events (upcoming only by default) with optional filters.</summary>
    /// <remarks>
    /// Public - no login needed. Query parameters (all optional): province, eventType (Run/Walk/Cycle),
    /// search (matches name or location), fromDate, toDate, includePast (default false).
    /// </remarks>
    /// <response code="200">Array of events ordered by date.</response>
    /// <response code="400">Invalid eventType filter.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<EventResponse>>> GetEvents(
        [FromQuery] string? province,
        [FromQuery] string? eventType,
        [FromQuery] string? search,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] bool includePast = false)
    {
        var query = _db.Events.AsNoTracking().Include(e => e.Organiser).AsQueryable();

        if (!includePast)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            query = query.Where(e => e.EventDate >= today);
        }
        if (!string.IsNullOrWhiteSpace(province))
            query = query.Where(e => e.Province == province);
        if (!string.IsNullOrWhiteSpace(eventType))
        {
            var type = EventTypes.Normalise(eventType);
            if (type == null) return BadRequest(new ErrorResponse("eventType must be Run, Walk or Cycle."));
            query = query.Where(e => e.EventType == type);
        }
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(e => e.EventName.Contains(search) || e.Location.Contains(search));
        if (fromDate.HasValue)
            query = query.Where(e => e.EventDate >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(e => e.EventDate <= toDate.Value);

        var events = await query.OrderBy(e => e.EventDate).ThenBy(e => e.StartTime).ToListAsync();
        return Ok(events.Select(e => EventResponse.From(e)).ToList());
    }

    /// <summary>Gets one event, including its categories.</summary>
    /// <remarks>Public - no login needed.</remarks>
    /// <response code="200">The event with nested categories.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> GetEvent(int id)
    {
        var ev = await _db.Events.AsNoTracking()
            .Include(e => e.Organiser)
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.EventID == id);

        if (ev == null) return NotFound(new ErrorResponse("Event not found."));
        return EventResponse.From(ev, includeCategories: true);
    }

    /// <summary>Creates a new event owned by the logged-in Organiser.</summary>
    /// <remarks>
    /// Body: eventName, description, eventDate (yyyy-MM-dd, not in the past), startTime (hh:mm:ss),
    /// location, province, eventType ("Run", "Walk" or "Cycle") and distanceKM.
    /// </remarks>
    /// <response code="201">The created event.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not an Organiser.</response>
    [HttpPost]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EventResponse>> CreateEvent(EventRequest request)
    {
        var type = EventTypes.Normalise(request.EventType);
        if (type == null) return BadRequest(new ErrorResponse("EventType must be Run, Walk or Cycle."));
        if (request.EventDate!.Value < DateOnly.FromDateTime(DateTime.Today))
            return BadRequest(new ErrorResponse("Event date cannot be in the past."));

        var ev = new Event
        {
            OrganiserID = CurrentUserId,
            EventName = request.EventName.Trim(),
            Description = request.Description,
            EventDate = request.EventDate.Value,
            StartTime = request.StartTime,
            Location = request.Location.Trim(),
            Province = request.Province,
            EventType = type,
            DistanceKM = request.DistanceKM
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();
        await _db.Entry(ev).Reference(e => e.Organiser).LoadAsync();

        return CreatedAtAction(nameof(GetEvent), new { id = ev.EventID }, EventResponse.From(ev));
    }

    /// <summary>Updates an event. Only the Organiser who owns it may do this.</summary>
    /// <remarks>Body: same fields as creating an event.</remarks>
    /// <response code="200">The updated event.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not an Organiser, or does not own this event.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> UpdateEvent(int id, EventRequest request)
    {
        var ev = await _db.Events.Include(e => e.Organiser).FirstOrDefaultAsync(e => e.EventID == id);
        if (ev == null) return NotFound(new ErrorResponse("Event not found."));
        if (ev.OrganiserID != CurrentUserId) return Forbidden("You can only modify your own events.");

        var type = EventTypes.Normalise(request.EventType);
        if (type == null) return BadRequest(new ErrorResponse("EventType must be Run, Walk or Cycle."));

        ev.EventName = request.EventName.Trim();
        ev.Description = request.Description;
        ev.EventDate = request.EventDate!.Value;
        ev.StartTime = request.StartTime;
        ev.Location = request.Location.Trim();
        ev.Province = request.Province;
        ev.EventType = type;
        ev.DistanceKM = request.DistanceKM;
        await _db.SaveChangesAsync();

        return EventResponse.From(ev);
    }

    /// <summary>Deletes an event and its categories. Only the owning Organiser may do this.</summary>
    /// <remarks>An event that already has enrolments cannot be deleted.</remarks>
    /// <response code="204">Event deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not an Organiser, or does not own this event.</response>
    /// <response code="404">Event does not exist.</response>
    /// <response code="409">Event has enrolments.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var ev = await _db.Events.Include(e => e.Categories).FirstOrDefaultAsync(e => e.EventID == id);
        if (ev == null) return NotFound(new ErrorResponse("Event not found."));
        if (ev.OrganiserID != CurrentUserId) return Forbidden("You can only delete your own events.");

        if (await _db.Enrolments.AnyAsync(e => e.Category.EventID == id))
            return Conflict(new ErrorResponse("This event already has enrolments and cannot be deleted."));

        _db.Categories.RemoveRange(ev.Categories);
        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>Lists every enrolment for an event (Organiser who owns it only).</summary>
    /// <response code="200">Array of enrolments with participant and category details.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not an Organiser, or does not own this event.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpGet("{id:int}/enrolments")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<EnrolmentResponse>>> GetEventEnrolments(int id)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventID == id);
        if (ev == null) return NotFound(new ErrorResponse("Event not found."));
        if (ev.OrganiserID != CurrentUserId) return Forbidden("You can only view enrolments for your own events.");

        var items = await _db.Enrolments.AsNoTracking().WithDetails()
            .Where(e => e.Category.EventID == id)
            .OrderBy(e => e.Category.CategoryName)
            .ThenBy(e => e.Participant.FullName)
            .ToListAsync();

        return Ok(items.Select(e => EnrolmentResponse.From(e)).ToList());
    }
}
