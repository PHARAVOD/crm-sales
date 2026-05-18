using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Crm.Database.Entities;

[Table("Tasks")]
public class CrmTask
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    public int? OrderId { get; set; }
    
    public int? LeadId { get; set; }
    
    [MaxLength(100)]
    public string? AssignedTo { get; set; }
    
    public string? Status { get; set; } = "pending";
    
    public DateTime? DueDate { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? CompletedAt { get; set; }
    
    [ForeignKey("OrderId")]
    public virtual Order? Order { get; set; }
    
    [ForeignKey("LeadId")]
    public virtual Lead? Lead { get; set; }
}