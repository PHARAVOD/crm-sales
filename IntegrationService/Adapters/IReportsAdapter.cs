using IntegrationService.Models;

namespace IntegrationService.Adapters;

public interface IReportsAdapter
{
    Task<List<ReportDto>> GetAllReportsAsync();
    Task<ReportDto> CreateSalesReportAsync(CreateReportRequest request);
}