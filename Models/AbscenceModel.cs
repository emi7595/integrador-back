namespace integrador_back.Models;

public class AbscenceModel
{
    public DateTime? date { get; set; }
    public string? startTime { get; set; }
    public int? idSchedule { get; set; }
    public int? idCode { get; set; }
}