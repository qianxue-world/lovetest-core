namespace ActivationCodeApi.Models;

public class ActivationCode
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public bool IsUsed { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
