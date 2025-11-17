using System.ComponentModel.DataAnnotations;

namespace ActivationCodeApi.DTOs;

public class ChangePasswordRequest
{
    [Required]
    public string OldPassword { get; set; } = string.Empty;
    
    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;
}
