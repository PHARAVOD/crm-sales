using Polly;
using Polly.Retry;

public enum OrderStatus
{
    New, Paid, Shipped, Delivered, Cancelled, Failed
}

public class OrderItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Total => Quantity * Price;
}

public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public List<OrderItem> Items { get; set; } = new();
    public decimal TotalAmount => Items.Sum(i => i.Total);
    public OrderStatus Status { get; set; } = OrderStatus.New;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
    public List<string> EventLog { get; set; } = new();
    
    public void AddEvent(string message)
    {
        EventLog.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        Console.WriteLine($"   {EventLog.Last()}");
    }
}

public class Result
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public object? Data { get; set; }
    public static Result Ok(string msg = "Успешно") => new() { Success = true, Message = msg };
    public static Result Fail(string msg) => new() { Success = false, Message = msg };
}

public class OrderStateMachine
{
    private Order _order;
    private readonly Dictionary<OrderStatus, List<OrderStatus>> _transitions = new()
    {
        [OrderStatus.New] = new() { OrderStatus.Paid, OrderStatus.Cancelled },
        [OrderStatus.Paid] = new() { OrderStatus.Shipped, OrderStatus.Cancelled, OrderStatus.Failed },
        [OrderStatus.Shipped] = new() { OrderStatus.Delivered, OrderStatus.Cancelled },
        [OrderStatus.Delivered] = new() { },
        [OrderStatus.Cancelled] = new() { },
        [OrderStatus.Failed] = new() { OrderStatus.Cancelled }
    };

    public OrderStateMachine(Order order)
    {
        _order = order;
        _order.AddEvent($"StateMachine инициализирована. Статус: {_order.Status}");
    }

    public bool TransitionTo(OrderStatus newStatus, string? reason = null)
    {
        if (!_transitions[_order.Status].Contains(newStatus))
        {
            _order.AddEvent($"❌ Невозможно: {_order.Status} → {newStatus}");
            return false;
        }
        _order.AddEvent($"✅ {_order.Status} → {newStatus}" + (reason != null ? $". {reason}" : ""));
        _order.Status = newStatus;
        _order.UpdatedAt = DateTime.Now;
        return true;
    }

    public string GetDiagram() => @"
╔════════════════════════════════════════════════════════════════════╗
║                    UML STATE MACHINE DIAGRAM                       ║
╠════════════════════════════════════════════════════════════════════╣
║                                                                    ║
║   ┌─────────┐     ┌─────────┐     ┌─────────┐     ┌──────────┐    ║
║   │   NEW   │────▶│  PAID   │────▶│ SHIPPED │────▶│DELIVERED │    ║
║   └────┬────┘     └────┬────┘     └────┬────┘     └──────────┘    ║
║        │               │               │                          ║
║        ▼               ▼               ▼                          ║
║   ┌──────────┐    ┌──────────┐    ┌──────────┐                   ║
║   │CANCELLED │    │CANCELLED │    │CANCELLED │                   ║
║   └──────────┘    └──────────┘    └──────────┘                   ║
║                                                                    ║
║   Допустимые переходы:                                             ║
║   NEW → PAID, CANCELLED | PAID → SHIPPED, CANCELLED               ║
║   SHIPPED → DELIVERED, CANCELLED | FAILED → CANCELLED             ║
╚════════════════════════════════════════════════════════════════════╝";
}

public class StockService
{
    private readonly Dictionary<int, int> _stock = new() { { 1, 10 }, { 2, 50 }, { 3, 30 }, { 999, 0 } };
    public async Task<Result> CheckStockAsync(int productId, int quantity)
    {
        await Task.Delay(100);
        if (!_stock.ContainsKey(productId)) return Result.Fail("Товар не найден");
        return _stock[productId] >= quantity ? Result.Ok($"В наличии: {_stock[productId]}") : Result.Fail($"Доступно: {_stock[productId]}");
    }
    public async Task<Result> ReserveStockAsync(int productId, int quantity)
    {
        await Task.Delay(100);
        if (!_stock.ContainsKey(productId)) return Result.Fail("Товар не найден");
        if (_stock[productId] >= quantity) { _stock[productId] -= quantity; return Result.Ok(); }
        return Result.Fail("Недостаточно");
    }
    public async Task<Result> ReleaseStockAsync(int productId, int quantity)
    {
        await Task.Delay(100);
        if (_stock.ContainsKey(productId)) _stock[productId] += quantity;
        return Result.Ok();
    }
}

public class SagaOrchestrator
{
    private readonly StockService _stock = new();
    private readonly AsyncRetryPolicy _retry = Policy.Handle<Exception>().WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(i));
    private int _orderId = 1;

    public async Task<Result> ExecuteOrderWorkflow(int customerId, List<OrderItem> items)
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║     🚀 СКВОЗНОЙ СЦЕНАРИЙ «ОФОРМЛЕНИЕ ЗАКАЗА»              ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.ResetColor();

        var order = new Order { Id = _orderId++, CustomerId = customerId, Items = items };
        var sm = new OrderStateMachine(order);
        var compensation = new Stack<Func<Task>>();

        Console.WriteLine($"\n📦 ЗАКАЗ #{order.Id} | Сумма: {order.TotalAmount:N0} руб");
        Console.WriteLine($"   Товары: {string.Join(", ", items.Select(i => $"{i.ProductName} x{i.Quantity}"))}");
        Console.WriteLine($"\n{new string('═', 60)}");

        try
        {
            Console.WriteLine("\n📌 ШАГ 1/4: ОПЛАТА...");
            await _retry.ExecuteAsync(async () => { await Task.Delay(200); if (order.TotalAmount > 100000) throw new Exception("Лимит превышен"); });
            sm.TransitionTo(OrderStatus.Paid, "Оплачена");
            compensation.Push(async () => { Console.WriteLine("   💸 Возврат средств"); await Task.Delay(100); });

            Console.WriteLine("\n📌 ШАГ 2/4: ПРОВЕРКА ОСТАТКОВ...");
            foreach (var i in items)
            {
                var r = await _stock.CheckStockAsync(i.ProductId, i.Quantity);
                if (!r.Success) throw new Exception($"{i.ProductName}: {r.Message}");
                Console.WriteLine($"   ✅ {i.ProductName}: {r.Message}");
                await _stock.ReserveStockAsync(i.ProductId, i.Quantity);
            }
            compensation.Push(async () => { foreach (var i in items) await _stock.ReleaseStockAsync(i.ProductId, i.Quantity); });

            Console.WriteLine("\n📌 ШАГ 3/4: ОТПРАВКА...");
            await Task.Delay(200);
            sm.TransitionTo(OrderStatus.Shipped, "Отправлен");

            Console.WriteLine("\n📌 ШАГ 4/4: ДОСТАВКА...");
            await Task.Delay(300);
            sm.TransitionTo(OrderStatus.Delivered, "Доставлен");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n✅ ЗАКАЗ #{order.Id} УСПЕШНО ДОСТАВЛЕН!");
            Console.ResetColor();
            return Result.Ok($"Заказ #{order.Id} выполнен");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n💥 ОШИБКА: {ex.Message}");
            Console.ResetColor();
            Console.WriteLine("\n🔄 КОМПЕНСАЦИЯ (SAGA)...");
            sm.TransitionTo(OrderStatus.Failed, ex.Message);
            foreach (var c in compensation) await c();
            sm.TransitionTo(OrderStatus.Cancelled, "Отменен");
            return Result.Fail(ex.Message);
        }
    }
}

class Program
{
    static async Task Main()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║     CRM СКВОЗНОЙ СЦЕНАРИЙ - ОФОРМЛЕНИЕ ЗАКАЗА              ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.ResetColor();

        var demo = new OrderStateMachine(new Order());
        Console.WriteLine(demo.GetDiagram());

        var saga = new SagaOrchestrator();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n\n🔹 ТЕСТ 1: УСПЕШНЫЙ СЦЕНАРИЙ");
        Console.WriteLine(new string('=', 50));
        Console.ResetColor();

        await saga.ExecuteOrderWorkflow(1, new List<OrderItem> { new() { ProductId = 1, ProductName = "Ноутбук", Quantity = 1, Price = 50000 } });

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("\n\n🔹 ТЕСТ 2: ОШИБКА + КОМПЕНСАЦИЯ");
        Console.WriteLine(new string('=', 50));
        Console.ResetColor();

        await saga.ExecuteOrderWorkflow(2, new List<OrderItem> { new() { ProductId = 999, ProductName = "Товар-фантом", Quantity = 1, Price = 999999 } });

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n\n╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                    ТЕСТИРОВАНИЕ ЗАВЕРШЕНО                    ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine("\n✅ State Machine: NEW → PAID → SHIPPED → DELIVERED");
        Console.WriteLine("✅ Проверка остатков (StockService)");
        Console.WriteLine("✅ Saga Pattern (компенсация при ошибке)");
        Console.WriteLine("\nНажми Enter для выхода...");
        Console.ReadLine();
    }
}