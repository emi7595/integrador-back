namespace integrador_back.Models;

public class SchoolAvg
{
    public string? schoolName { get; set; }
    public int? departmentId { get; set; }
    public string? departmentName { get; set; }
    public double? average { get; set; }
    public int[]? codes { get; set; }

    // Constructor
    public SchoolAvg()
    {
        codes = new int[11];
    }
}
