using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Crm.Database.Entities;

[Table("CartItems")]
public class CartItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    public int ProductId { get; set; }
    
    [MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }
    
    public int Quantity { get; set; } = 1;
    
    public int? LeadId { get; set; }
    
    public string? SessionId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [ForeignKey("ProductId")]
    public virtual Product? Product { get; set; }
    
    [ForeignKey("LeadId")]
    public virtual Lead? Lead { get; set; }
}