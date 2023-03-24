namespace integrador_back.Models;

public class DepartmentAvg
{
    public string? employeeName { get; set; }
    public string? nomina { get; set; }
    public string? departmentName { get; set; }
    public double? average { get; set; }
    public int[]? codes { get; set; }

    // Constructor
    public DepartmentAvg()
    {
        codes = new int[11];
    }
}
