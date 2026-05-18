using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Crm.Database.Entities;

[Table("OrderLines")]
public class OrderLine
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    public int OrderId { get; set; }
    
    public int ProductId { get; set; }
    
    [MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }
    
    public int Quantity { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPrice => UnitPrice * Quantity;
    
    [ForeignKey("OrderId")]
    public virtual Order? Order { get; set; }
    
    [ForeignKey("ProductId")]
    public virtual Product? Product { get; set; }
}