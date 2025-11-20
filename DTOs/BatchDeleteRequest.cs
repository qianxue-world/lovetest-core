using System.ComponentModel.DataAnnotations;

namespace ActivationCodeApi.DTOs;

public class BatchDeleteRequest
{
    [Required]
    public string Pattern { get; set; } = string.Empty;
    
    public bool DryRun { get; set; } = false;
}
