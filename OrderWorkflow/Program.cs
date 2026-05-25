using Polly;
using Polly.Retry;
using Serilog;

// =====================================================
// МОДЕЛИ
// =====================================================

public enum OrderStatus { New, Paid, Shipped, Delivered, Cancelled, Failed }

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
    public List<string> EventLog { get; set; } = new();
    
    public void AddEvent(string message)
    {
        EventLog.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        Log.Information("{Message}", message);
    }
}

public class Result
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public static Result Ok(string msg = "Успешно") => new() { Success = true, Message = msg };
    public static Result Fail(string msg) => new() { Success = false, Message = msg };
}

// =====================================================
// STATE MACHINE
// =====================================================

public class OrderStateMachine
{
    private Order _order;
    private Dictionary<OrderStatus, List<OrderStatus>> _transitions = new()
    {
        [OrderStatus.New] = new() { OrderStatus.Paid, OrderStatus.Cancelled },
        [OrderStatus.Paid] = new() { OrderStatus.Shipped, OrderStatus.Cancelled, OrderStatus.Failed },
        [OrderStatus.Shipped] = new() { OrderStatus.Delivered, OrderStatus.Cancelled },
        [OrderStatus.Delivered] = new() { },
        [OrderStatus.Cancelled] = new() { },
        [OrderStatus.Failed] = new() { OrderStatus.Cancelled }
    };

    public OrderStateMachine(Order order) { _order = order; _order.AddEvent($"Статус: {_order.Status}"); }

    public bool TransitionTo(OrderStatus newStatus, string? reason = null)
    {
        if (!_transitions[_order.Status].Contains(newStatus))
        {
            _order.AddEvent($"❌ Невозможно: {_order.Status} → {newStatus}");
            return false;
        }
        _order.AddEvent($"✅ {_order.Status} → {newStatus}" + (reason != null ? $". {reason}" : ""));
        _order.Status = newStatus;
        return true;
    }
}

// =====================================================
// СЕРВИСЫ
// =====================================================

public class StockService
{
    private Dictionary<int, int> _stock = new() { { 1, 10 }, { 2, 50 }, { 3, 30 }, { 999, 0 } };
    
    public async Task<Result> CheckStockAsync(int productId, int quantity)
    {
        await Task.Delay(100);
        if (!_stock.ContainsKey(productId)) return Result.Fail("Товар не найден");
        return _stock[productId] >= quantity ? Result.Ok($"В наличии: {_stock[productId]}") : Result.Fail($"Доступно: {_stock[productId]}");
    }
}

public class PaymentService
{
    public async Task<Result> ProcessPaymentAsync(decimal amount, int orderId)
    {
        await Task.Delay(500);
        if (amount > 150000) return Result.Fail("Превышен лимит оплаты");
        return Result.Ok($"Платеж {amount} руб одобрен");
    }
}

// =====================================================
// SAGA ORCHESTRATOR
// =====================================================

public class SagaOrchestrator
{
    private readonly PaymentService _payment = new();
    private readonly StockService _stock = new();
    private readonly AsyncRetryPolicy<Result> _retry;
    private int _orderId = 1;

    public SagaOrchestrator()
    {
        _retry = Policy<Result>
            .Handle<Exception>()
            .OrResult(r => !r.Success)
            .WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(i),
                onRetry: (outcome, ts, retry, ctx) => 
                    Log.Warning("Retry {RetryCount} через {Delay} сек", retry, ts.Seconds));
    }

    public async Task<Result> ExecuteOrderWorkflow(int customerId, List<OrderItem> items)
    {
        var order = new Order { Id = _orderId++, CustomerId = customerId, Items = items };
        var sm = new OrderStateMachine(order);
        var compensation = new Stack<Func<Task>>();

        Log.Information("══════════════════════════════════════");
        Log.Information("🚀 ЗАКАЗ #{OrderId} | Сумма: {Amount:N0} руб", order.Id, order.TotalAmount);
        Log.Information("══════════════════════════════════════");

        try
        {
            Log.Information("📌 ШАГ 1/4: Оплата...");
            var payment = await _retry.ExecuteAsync(() => _payment.ProcessPaymentAsync(order.TotalAmount, order.Id));
            if (!payment.Success) throw new Exception(payment.Message);
            sm.TransitionTo(OrderStatus.Paid, payment.Message);
            compensation.Push(async () => Log.Warning("💸 Возврат {Amount}", order.TotalAmount));

            Log.Information("📌 ШАГ 2/4: Проверка остатков...");
            foreach (var item in order.Items)
            {
                var stockResult = await _stock.CheckStockAsync(item.ProductId, item.Quantity);
                if (!stockResult.Success) throw new Exception($"{item.ProductName}: {stockResult.Message}");
                Log.Information("   ✅ {ProductName}", item.ProductName);
            }

            Log.Information("📌 ШАГ 3/4: Отправка...");
            await Task.Delay(300);
            sm.TransitionTo(OrderStatus.Shipped, "Отправлен");

            Log.Information("📌 ШАГ 4/4: Доставка...");
            await Task.Delay(300);
            sm.TransitionTo(OrderStatus.Delivered, "Доставлен");

            Log.Information("🎉 ЗАКАЗ #{OrderId} ВЫПОЛНЕН!", order.Id);
            return Result.Ok($"Заказ #{order.Id} выполнен");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "💥 ОШИБКА: {Message}", ex.Message);
            sm.TransitionTo(OrderStatus.Failed, ex.Message);
            foreach (var c in compensation) await c();
            sm.TransitionTo(OrderStatus.Cancelled, "Отменен");
            return Result.Fail(ex.Message);
        }
    }
}

// =====================================================
// MAIN
// =====================================================

class Program
{
    static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message}{NewLine}")
            .WriteTo.File("logs/crm-log-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        Log.Information("╔════════════════════════════════════════╗");
        Log.Information("║     CRM - ОБРАБОТКА ОШИБОК            ║");
        Log.Information("║     Serilog + Polly (Retry)           ║");
        Log.Information("╚════════════════════════════════════════╝");

        var saga = new SagaOrchestrator();

        Log.Information("\n🔹 ТЕСТ 1: Успешный заказ");
        await saga.ExecuteOrderWorkflow(1, new List<OrderItem> { new() { ProductId = 1, ProductName = "Ноутбук", Quantity = 1, Price = 50000 } });

        Log.Information("\n🔹 ТЕСТ 2: Ошибка + Retry + Компенсация");
        await saga.ExecuteOrderWorkflow(2, new List<OrderItem> { new() { ProductId = 1, ProductName = "Ноутбук", Quantity = 1, Price = 200000 } });

        Log.Information("\n✅ Тестирование завершено!");
        Console.WriteLine("\nНажми Enter...");
        Console.ReadLine();
        Log.CloseAndFlush();
    }
}