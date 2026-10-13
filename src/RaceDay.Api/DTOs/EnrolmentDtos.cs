using RaceDay.Api.Models;

namespace RaceDay.Api.DTOs;

public class ResultSummary
{
    public int ResultID { get; set; }
    public TimeOnly? FinishTime { get; set; }
    public int? OverallPosition { get; set; }
    public int? CategoryPosition { get; set; }
}

public class EnrolmentResponse
{
    public int EnrolmentID { get; set; }
    public int ParticipantID { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int EventID { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public ResultSummary? Result { get; set; }

    /// <summary>Expects Participant, Category.Event and Result to be loaded.</summary>
    public static EnrolmentResponse From(Enrolment e) => new()
    {
        EnrolmentID = e.EnrolmentID,
        ParticipantID = e.ParticipantID,
        ParticipantName = e.Participant.FullName,
        CategoryID = e.CategoryID,
        CategoryName = e.Category.CategoryName,
        EventID = e.Category.EventID,
        EventName = e.Category.Event.EventName,
        EventDate = e.Category.Event.EventDate,
        EnrolmentDate = e.EnrolmentDate,
        Status = e.Status,
        Result = e.Result == null ? null : new ResultSummary
        {
            ResultID = e.Result.ResultID,
            FinishTime = e.Result.FinishTime,
            OverallPosition = e.Result.OverallPosition,
            CategoryPosition = e.Result.CategoryPosition
        }
    };
}
