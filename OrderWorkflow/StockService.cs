namespace OrderWorkflow;

public class StockService
{
    // Симуляция базы данных товаров с остатками
    private readonly Dictionary<int, int> _stock = new()
    {
        { 1, 10 },   // Ноутбук: 10 шт
        { 2, 50 },   // Мышь: 50 шт
        { 3, 30 },   // Клавиатура: 30 шт
        { 4, 5 },    // Монитор: 5 шт
        { 999, 0 }   // Тестовый товар: 0 шт
    };

    public async Task<Result> CheckStockAsync(int productId, int quantity)
    {
        await Task.Delay(100); // Симуляция запроса к БД
        
        if (!_stock.ContainsKey(productId))
            return Result.Fail($"Товар с ID {productId} не найден");
        
        var available = _stock[productId];
        
        if (available >= quantity)
            return Result.Ok($"Товар в наличии: {available} шт");
        
        return Result.Fail($"Недостаточно товара. Запрошено: {quantity}, доступно: {available}");
    }
    
    public async Task<Result> ReserveStockAsync(int productId, int quantity)
    {
        await Task.Delay(100);
        
        if (!_stock.ContainsKey(productId))
            return Result.Fail("Товар не найден");
        
        if (_stock[productId] >= quantity)
        {
            _stock[productId] -= quantity;
            return Result.Ok($"Товар зарезервирован. Остаток: {_stock[productId]}");
        }
        
        return Result.Fail("Недостаточно товара для резерва");
    }
    
    public async Task<Result> ReleaseStockAsync(int productId, int quantity)
    {
        await Task.Delay(100);
        
        if (!_stock.ContainsKey(productId))
            return Result.Fail("Товар не найден");
        
        _stock[productId] += quantity;
        return Result.Ok($"Резерв отменен. Остаток: {_stock[productId]}");
    }
}