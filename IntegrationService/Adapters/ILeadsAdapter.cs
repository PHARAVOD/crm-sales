using IntegrationService.Models;

namespace IntegrationService.Adapters;

public interface ILeadsAdapter
{
    Task<List<LeadDto>> GetAllLeadsAsync();
    Task<LeadDto> GetLeadByIdAsync(int id);
    Task<LeadDto> CreateLeadAsync(CreateLeadRequest request);
}