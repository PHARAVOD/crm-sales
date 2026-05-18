using IntegrationService.Models;

namespace IntegrationService;

public interface ICrmOrchestrator
{
    Task<OrderResult> ProcessOrderFromCartAsync(int leadId);
    Task<OrderResult> ProcessFullCrmWorkflowAsync(CreateLeadRequest leadRequest);
    Task<ReportDto> GenerateDailyReportAsync();
    Task<string> GetSystemStatusAsync();
}