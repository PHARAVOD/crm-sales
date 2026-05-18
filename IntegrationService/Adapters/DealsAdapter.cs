using System.Text;
using System.Text.Json;
using IntegrationService.Models;
using Microsoft.Extensions.Logging;

namespace IntegrationService.Adapters;

public class DealsAdapter : IDealsAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DealsAdapter> _logger;
    private const string BaseUrl = "http://localhost:3000";

    public DealsAdapter(HttpClient httpClient, ILogger<DealsAdapter> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<DealDto>> GetAllDealsAsync()
    {
        var response = await _httpClient.GetAsync($"{BaseUrl}/deals");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<DealDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public async Task<DealDto> CreateDealAsync(CreateDealRequest request)
    {
        try
        {
            _logger.LogInformation("📡 Вызов модуля В (Сделки): POST /deals/create, сумма={Amount}", request.Amount);
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{BaseUrl}/deals/create", content);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var deal = JsonSerializer.Deserialize<DealDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            _logger.LogInformation("✅ Создана сделка #{DealId} на сумму {Amount}", deal?.Id, deal?.Amount);
            return deal;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Ошибка создания сделки");
            throw;
        }
    }

    public async Task<DealDto> UpdateDealStageAsync(int dealId, string stage)
    {
        var content = new StringContent(JsonSerializer.Serialize(new { stage }), Encoding.UTF8, "application/json");
        var response = await _httpClient.PatchAsync($"{BaseUrl}/deals/{dealId}/stage", content);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<DealDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
}