using System.ComponentModel.DataAnnotations;

namespace ActivationCodeApi.DTOs;

public class GenerateCodesRequest
{
    [Required]
    [Range(1, 20000)]
    public int Count { get; set; }
    
    public string? Prefix { get; set; }
}
