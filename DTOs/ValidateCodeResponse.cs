namespace ActivationCodeApi.DTOs;

public class ValidateCodeResponse
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
}
