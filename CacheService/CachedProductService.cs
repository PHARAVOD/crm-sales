using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

public interface IProductCacheService
{
    Task<List<Product>> GetProductsAsync();
    Task<Product> GetProductByIdAsync(int id);
    Task InvalidateCacheAsync();
}

public class ProductCacheService : IProductCacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<ProductCacheService> _logger;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

    public ProductCacheService(IDistributedCache cache, ILogger<ProductCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<Product>> GetProductsAsync()
    {
        const string cacheKey = "products:all";
        
        // 1. Пытаемся получить из кэша
        var cachedData = await _cache.GetStringAsync(cacheKey);
        
        if (cachedData != null)
        {
            _logger.LogInformation("✅ Данные получены из кэша (Redis)");
            return JsonSerializer.Deserialize<List<Product>>(cachedData);
        }

        // 2. Кэш пуст - загружаем из БД
        _logger.LogInformation("📦 Кэш пуст, загрузка из БД...");
        var products = await GetProductsFromDatabase();
        
        // 3. Сохраняем в кэш
        var serialized = JsonSerializer.Serialize(products);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _cacheDuration
        };
        
        await _cache.SetStringAsync(cacheKey, serialized, options);
        _logger.LogInformation("💾 Данные сохранены в кэш на {Minutes} минут", _cacheDuration.TotalMinutes);
        
        return products;
    }

    public async Task<Product> GetProductByIdAsync(int id)
    {
        var cacheKey = $"product:{id}";
        var cachedData = await _cache.GetStringAsync(cacheKey);
        
        if (cachedData != null)
        {
            return JsonSerializer.Deserialize<Product>(cachedData);
        }

        var product = await GetProductFromDatabaseById(id);
        
        if (product != null)
        {
            var serialized = JsonSerializer.Serialize(product);
            await _cache.SetStringAsync(cacheKey, serialized, 
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) });
        }
        
        return product;
    }

    public async Task InvalidateCacheAsync()
    {
        await _cache.RemoveAsync("products:all");
        _logger.LogInformation("🗑️ Кэш инвалидирован");
    }

    private Task<List<Product>> GetProductsFromDatabase()
    {
        // Симуляция запроса к БД (500 мс)
        return Task.Delay(500).ContinueWith(_ => new List<Product>
        {
            new() { Id = 1, Name = "Ноутбук", Price = 50000 },
            new() { Id = 2, Name = "Мышь", Price = 1000 },
            new() { Id = 3, Name = "Клавиатура", Price = 3000 }
        });
    }

    private Task<Product> GetProductFromDatabaseById(int id)
    {
        return Task.Delay(100).ContinueWith(_ => new Product { Id = id, Name = "Товар", Price = 10000 });
    }
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}