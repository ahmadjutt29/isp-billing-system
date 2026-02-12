using System.ComponentModel.DataAnnotations;

namespace IspBackend.DTOs;

public class PayRequestDto
{
    [Required]
    public string TransactionId { get; set; } = string.Empty;

    [Required]
    public string PayeeName { get; set; } = string.Empty;

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero")]
    public decimal Amount { get; set; }

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
}
