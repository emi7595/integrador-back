namespace integrador_back.Models;

public class Course
{
    public int? currentClass { get; set; }
    public string? CRN { get; set; }
    public string? subject_CVE { get; set; }
    public string? subjectName { get; set; }
    public string? startHour { get; set; }
    public string? endHour { get; set; }
}