namespace RaceDay.Api.Models;

public class Result
{
    public int ResultID { get; set; }
    public int EnrolmentID { get; set; }
    public int CapturedByOrganiserID { get; set; }
    public TimeOnly? FinishTime { get; set; }
    public int? OverallPosition { get; set; }
    public int? CategoryPosition { get; set; }
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;

    public Enrolment Enrolment { get; set; } = null!;
    public AppUser CapturedByOrganiser { get; set; } = null!;
}
