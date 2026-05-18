using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Crm.Database.Entities;

[Table("Orders")]
public class Order
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    public int? LeadId { get; set; }
    
    public int? ContactId { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }
    
    public string? Stage { get; set; } = "new";
    
    public string? Status { get; set; } = "active";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public DateTime? ClosedAt { get; set; }
    
    [ForeignKey("LeadId")]
    public virtual Lead? Lead { get; set; }
}