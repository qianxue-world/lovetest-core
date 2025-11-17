namespace ActivationCodeApi.DTOs;

public class CodeStatsResponse
{
    public int TotalCodes { get; set; }
    public int UnusedCodes { get; set; }
    public int UsedCodes { get; set; }
    public int ActiveCodes { get; set; }
}
