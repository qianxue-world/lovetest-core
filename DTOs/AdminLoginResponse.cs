namespace ActivationCodeApi.DTOs;

public class AdminLoginResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Token { get; set; }
}
