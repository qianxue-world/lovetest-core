using System.ComponentModel.DataAnnotations;

namespace ActivationCodeApi.DTOs;

public class ValidateCodeRequest
{
    [Required]
    public string Code { get; set; } = string.Empty;
}
