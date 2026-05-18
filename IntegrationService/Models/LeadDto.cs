namespace IntegrationService.Models;

public class LeadDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; }
    public string Status { get; set; }
    public string Source { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateLeadRequest
{
    public string Name { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; }
    public string Source { get; set; }
}