namespace IntegrationService.Models;

public class OrderLine
{
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice => Quantity * UnitPrice;
}

public class OrderRequest
{
    public int LeadId { get; set; }
    public int? ContactId { get; set; }
    public List<OrderLine> OrderLines { get; set; }
    public decimal TotalAmount => OrderLines?.Sum(x => x.TotalPrice) ?? 0;
}

public class OrderResult
{
    public int DealId { get; set; }
    public int TaskId { get; set; }
    public int ReportId { get; set; }
    public string Status { get; set; }
    public string Message { get; set; }
}