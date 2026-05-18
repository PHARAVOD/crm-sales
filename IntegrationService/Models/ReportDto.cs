namespace IntegrationService.Models;

public class ReportDto
{
    public int Id { get; set; }
    public string Period { get; set; }
    public decimal TotalSales { get; set; }
    public int DealsCount { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public class CreateReportRequest
{
    public string Period { get; set; }
    public decimal Total { get; set; }
    public int DealsCount { get; set; }
}