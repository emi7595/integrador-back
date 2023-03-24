namespace integrador_back.Models;

public class ProfessorAttendanceAvg
{
    public string? nomina { get; set; }
    public string? employeeName { get; set; }
    public string? subjectName { get; set; }
    public string? CRN { get; set; }
    public string? subject_CVE { get; set; }
    public string? scheduleId { get; set; }
    public double? average { get; set; }
    public int[]? codes { get; set; }

    // Constructor
    public ProfessorAttendanceAvg()
    {
        codes = new int[11];
    }
}
