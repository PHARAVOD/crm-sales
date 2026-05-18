namespace IntegrationService.Models;

public class DealDto
{
    public int Id { get; set; }
    public int? ContactId { get; set; }
    public int? LeadId { get; set; }
    public decimal Amount { get; set; }
    public string Stage { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; }
}

public class CreateDealRequest
{
    public int? ContactId { get; set; }
    public int? LeadId { get; set; }
    public decimal Amount { get; set; }
    public string Stage { get; set; }
}