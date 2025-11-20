namespace ActivationCodeApi.DTOs;

public class BatchDeleteResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int MatchedCount { get; set; }
    public int DeletedCount { get; set; }
    public List<string> MatchedCodes { get; set; } = new();
    public bool WasDryRun { get; set; }
}
