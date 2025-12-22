using System.ComponentModel.DataAnnotations;

namespace IspBackend.DTOs;

/// <summary>
/// DTO for fee response.
/// </summary>
public class FeeDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? Username { get; set; }
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public bool Paid { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for creating a new fee.
/// </summary>
public class CreateFeeDto
{
    [Required(ErrorMessage = "UserId is required")]
    public int UserId { get; set; }

    [Required(ErrorMessage = "Amount is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "DueDate is required")]
    public DateTime DueDate { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }
}

/// <summary>
/// DTO for marking a fee as paid.
/// </summary>
public class PayFeeDto
{
    public DateTime? PaymentDate { get; set; }
}

/// <summary>
/// Income summary DTO.
/// </summary>
public class IncomeSummaryDto
{
    public decimal TotalPaidAmount { get; set; }
    public decimal TotalUnpaidAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public int PaidFeesCount { get; set; }
    public int UnpaidFeesCount { get; set; }
    public int TotalFeesCount { get; set; }
    public int OverdueFeesCount { get; set; }
    public decimal OverdueAmount { get; set; }
    public decimal CollectionRate { get; set; }
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Monthly income DTO.
/// </summary>
public class MonthlyIncomeDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int FeesCount { get; set; }
}
