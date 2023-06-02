namespace integrador_back.Models;

public class VicerrectoriaAvg
{
    public string? vicerrectoriaName { get; set; }
    public int? schoolId { get; set; }
    public string? schoolName { get; set; }
    public double? average { get; set; }
    public int[]? codes { get; set; }

    // Constructor
    public VicerrectoriaAvg()
    {
        codes = new int[11];
    }
}
