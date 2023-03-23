namespace integrador_back.Models;

public class ProfessorAttendanceAvg
{
    public string? nomina { get; set; }
    public string? nombreEmpleado { get; set; }
    public string? materia { get; set; }
    public string? CRN { get; set; }
    public string? CVE_Materia { get; set; }
    public string? idHorario { get; set; }
    public double? average { get; set; }
    public int[]? codes { get; set; }

    // Constructor
    public ProfessorAttendanceAvg()
    {
        codes = new int[11];
    }
}
