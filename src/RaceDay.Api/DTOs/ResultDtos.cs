using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.DTOs;

public class ResultRequest
{
    /// <summary>Finish time as hh:mm:ss, e.g. "01:45:32".</summary>
    [Required]
    public TimeOnly? FinishTime { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int? OverallPosition { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int? CategoryPosition { get; set; }
}

public class ResultResponse
{
    public int ResultID { get; set; }
    public int EnrolmentID { get; set; }
    public int ParticipantID { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int EventID { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public TimeOnly? FinishTime { get; set; }
    public int? OverallPosition { get; set; }
    public int? CategoryPosition { get; set; }

    /// <summary>Expects Enrolment.Participant and Enrolment.Category.Event to be loaded.</summary>
    public static ResultResponse From(Result r) => new()
    {
        ResultID = r.ResultID,
        EnrolmentID = r.EnrolmentID,
        ParticipantID = r.Enrolment.ParticipantID,
        ParticipantName = r.Enrolment.Participant.FullName,
        EventID = r.Enrolment.Category.EventID,
        EventName = r.Enrolment.Category.Event.EventName,
        EventDate = r.Enrolment.Category.Event.EventDate,
        CategoryID = r.Enrolment.CategoryID,
        CategoryName = r.Enrolment.Category.CategoryName,
        FinishTime = r.FinishTime,
        OverallPosition = r.OverallPosition,
        CategoryPosition = r.CategoryPosition
    };
}
