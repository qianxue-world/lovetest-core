using ActivationCodeApi.Models;

namespace ActivationCodeApi.DTOs;

public class PagedCodesResponse
{
    public List<ActivationCode> Codes { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageSize { get; set; }
    public int? NextSkipToken { get; set; }
    public bool HasMore { get; set; }
}
