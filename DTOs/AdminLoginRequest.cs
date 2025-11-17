using System.ComponentModel.DataAnnotations;

namespace ActivationCodeApi.DTOs;

public class AdminLoginRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    public string Password { get; set; } = string.Empty;
}
