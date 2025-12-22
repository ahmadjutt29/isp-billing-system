using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IspBackend.Models;

/// <summary>
/// Represents a fee/billing record for a user.
/// </summary>
[Table("Fees")]
public class Fee
{
    /// <summary>
    /// Unique identifier for the fee.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Foreign key reference to the User who owes this fee.
    /// </summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// Navigation property to the associated User.
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }

    /// <summary>
    /// The amount of the fee.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero")]
    public decimal Amount { get; set; }

    /// <summary>
    /// The date when the fee is due.
    /// </summary>
    [Required]
    public DateTime DueDate { get; set; }

    /// <summary>
    /// Indicates whether the fee has been paid.
    /// </summary>
    public bool Paid { get; set; } = false;

    /// <summary>
    /// The date when the fee was paid (null if unpaid).
    /// </summary>
    public DateTime? PaymentDate { get; set; }

    /// <summary>
    /// Description or notes about the fee.
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Date and time when the fee record was created.
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date and time when the fee record was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
