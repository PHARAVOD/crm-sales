using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Crm.Database.Entities;

[Table("Reports")]
public class Report
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [MaxLength(50)]
    public string? Period { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalSales { get; set; }
    
    public int TotalOrders { get; set; }
    
    public int TotalLeads { get; set; }
    
    public int TotalTasks { get; set; }
    
    public string? ReportData { get; set; }
    
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}