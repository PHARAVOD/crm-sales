using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Crm.Database.Entities;

[Table("Leads")]
public class Lead
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string? Phone { get; set; }
    
    [MaxLength(200)]
    public string? Email { get; set; }
    
    [MaxLength(100)]
    public string? Company { get; set; }
    
    public string? Source { get; set; }
    
    public string? Status { get; set; } = "new";
    
    public int Score { get; set; } = 0;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
}