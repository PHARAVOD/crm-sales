using IntegrationService.Models;

namespace IntegrationService.Adapters;

public interface IDealsAdapter
{
    Task<List<DealDto>> GetAllDealsAsync();
    Task<DealDto> CreateDealAsync(CreateDealRequest request);
    Task<DealDto> UpdateDealStageAsync(int dealId, string stage);
}