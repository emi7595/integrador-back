namespace integrador_back.Models;

public class Course
{
    public int? currentClass { get; set; }
    public string? CRN { get; set; }
    public string? subject_CVE { get; set; }
    public string? subjectName { get; set; }
    public TimeSpan? startHour { get; set; }
    public TimeSpan? endHour { get; set; }
}