using System.Text;
using System.Text.Json;
using IntegrationService.Models;
using Microsoft.Extensions.Logging;

namespace IntegrationService.Adapters;

public class LeadsAdapter : ILeadsAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LeadsAdapter> _logger;
    private const string BaseUrl = "http://localhost:5001";

    public LeadsAdapter(HttpClient httpClient, ILogger<LeadsAdapter> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<LeadDto>> GetAllLeadsAsync()
    {
        try
        {
            _logger.LogInformation("📡 Вызов модуля А (Лиды): GET /leads");
            var response = await _httpClient.GetAsync($"{BaseUrl}/leads");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var leads = JsonSerializer.Deserialize<List<LeadDto>>(json, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });
            _logger.LogInformation("✅ Получено {Count} лидов", leads?.Count ?? 0);
            return leads ?? new List<LeadDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Ошибка получения лидов");
            throw;
        }
    }

    public async Task<LeadDto> GetLeadByIdAsync(int id)
    {
        var response = await _httpClient.GetAsync($"{BaseUrl}/leads/{id}");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<LeadDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public async Task<LeadDto> CreateLeadAsync(CreateLeadRequest request)
    {
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{BaseUrl}/leads", content);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<LeadDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
}