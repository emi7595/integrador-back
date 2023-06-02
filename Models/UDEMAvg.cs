namespace integrador_back.Models;

public class UDEMAvg
{
    public int? vicerrectoriaId { get; set; }
    public string? vicerrectoriaName { get; set; }
    public double? average { get; set; }
    public int[]? codes { get; set; }

    // Constructor
    public UDEMAvg()
    {
        codes = new int[11];
    }
}
