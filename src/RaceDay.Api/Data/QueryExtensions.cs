using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public static class QueryExtensions
{
    /// <summary>Includes everything needed to build an EnrolmentResponse.</summary>
    public static IQueryable<Enrolment> WithDetails(this IQueryable<Enrolment> query) =>
        query.Include(e => e.Participant)
             .Include(e => e.Category).ThenInclude(c => c.Event)
             .Include(e => e.Result);

    /// <summary>Includes everything needed to build a ResultResponse.</summary>
    public static IQueryable<Result> WithDetails(this IQueryable<Result> query) =>
        query.Include(r => r.Enrolment).ThenInclude(e => e.Participant)
             .Include(r => r.Enrolment).ThenInclude(e => e.Category).ThenInclude(c => c.Event);
}
