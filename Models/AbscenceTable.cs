namespace integrador_back.Models;

public class AbscenceTable
{
    public int? idReposition { get; set; }
    public string? subjectName { get; set; }
    public string? subject_CVE { get; set; }
    public DateTime? date { get; set; }
    public string? startTime { get; set; }
    public string? classroom { get; set; }
    public int? eventNum { get; set; }
    public int? idSchedule { get; set; }
    public int? idCode { get; set; }
}