using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace IspBackend.Models;

public class PayRequest
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int FeeId { get; set; }

    [ForeignKey(nameof(FeeId))]
    public virtual Fee? Fee { get; set; }

    [Required]
    public string TransactionId { get; set; } = string.Empty;

    [Required]
    public string PayeeName { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public bool Approved { get; set; } = false;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
}
