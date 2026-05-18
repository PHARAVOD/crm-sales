using System.Text;
using System.Text.Json;
using IntegrationService.Models;
using Microsoft.Extensions.Logging;

namespace IntegrationService.Adapters;

public class CartAdapter : ICartAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CartAdapter> _logger;
    private const string BaseUrl = "http://localhost:5002";

    public CartAdapter(HttpClient httpClient, ILogger<CartAdapter> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CartDto> GetCartAsync()
    {
        try
        {
            _logger.LogInformation("📡 Вызов модуля Б (Корзина): GET /cart");
            var response = await _httpClient.GetAsync($"{BaseUrl}/cart");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var items = JsonSerializer.Deserialize<List<CartItemDto>>(json, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });
            _logger.LogInformation("✅ В корзине {Count} товаров", items?.Count ?? 0);
            return new CartDto { Items = items ?? new List<CartItemDto>() };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Ошибка получения корзины");
            throw;
        }
    }

    public async Task<CartItemDto> AddToCartAsync(CartItemDto item)
    {
        var content = new StringContent(JsonSerializer.Serialize(item), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{BaseUrl}/cart/add", content);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var items = JsonSerializer.Deserialize<List<CartItemDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return items?.LastOrDefault();
    }

    public async Task ClearCartAsync()
    {
        var response = await _httpClient.PostAsync($"{BaseUrl}/cart/clear", null);
        response.EnsureSuccessStatusCode();
        _logger.LogInformation("🗑️ Корзина очищена");
    }
}