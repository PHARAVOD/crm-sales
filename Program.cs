using Polly;
using Polly.Retry;

// =====================================================
// МОДЕЛИ
// =====================================================

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

// =====================================================
// STATE MACHINE
// =====================================================

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

    public bool CanTransitionTo(OrderStatus newStatus)
    {
        if (!_transitions.ContainsKey(_order.Status))
            return false;
        return _transitions[_order.Status].Contains(newStatus);
    }

    public bool TransitionTo(OrderStatus newStatus, string? reason = null)
    {
        if (!CanTransitionTo(newStatus))
        {
            _order.AddEvent($"❌ Невозможно: {_order.Status} → {newStatus}");
            return false;
        }

        var oldStatus = _order.Status;
        _order.Status = newStatus;
        _order.UpdatedAt = DateTime.Now;
        
        string msg = $"✅ {oldStatus} → {newStatus}";
        if (!string.IsNullOrEmpty(reason)) msg += $". {reason}";
        _order.AddEvent(msg);
        
        return true;
    }

    public string GetDiagram()
    {
        return @"
╔════════════════════════════════════════════════════════════════════╗
║                    UML STATE MACHINE DIAGRAM                       ║
║                      Статусы заказа (Order)                        ║
╠════════════════════════════════════════════════════════════════════╣
║                                                                    ║
║                         ┌─────────┐                                ║
║                         │  START  │                                ║
║                         └────┬────┘                                ║
║                              │                                     ║
║                              ▼                                     ║
║   ┌──────────────────────────────────────────────────────────┐    ║
║   │                    ┌─────────┐                           │    ║
║   │                ┌──▶│   NEW   │                           │    ║
║   │                │   └────┬────┘                           │    ║
║   │          Отмена│        │ Оплата                         │    ║
║   │                │        ▼                                │    ║
║   │                │   ┌─────────┐    ┌─────────┐           │    ║
║   │                └──▶│  PAID   │───▶│ FAILED  │           │    ║
║   │                    └────┬────┘    └────┬────┘           │    ║
║   │                         │              │                 │    ║
║   │                   Отправка              │ Отмена          │    ║
║   │                         │              │                 │    ║
║   │                         ▼              ▼                 │    ║
║   │                    ┌─────────┐    ┌──────────┐           │    ║
║   │                    │ SHIPPED │───▶│CANCELLED │           │    ║
║   │                    └────┬────┘    └──────────┘           │    ║
║   │                         │                                 │    ║
║   │                   Доставка                                 │    ║
║   │                         ▼                                 │    ║
║   │                    ┌──────────┐                           │    ║
║   │                    │DELIVERED │                           │    ║
║   │                    └──────────┘                           │    ║
║   └──────────────────────────────────────────────────────────┘    ║
║                                                                    ║
╠════════════════════════════════════════════════════════════════════╣
║  Допустимые переходы:                                              ║
║  • NEW     → PAID (оплата успешна)                                 ║
║  • NEW     → CANCELLED (отмена)                                    ║
║  • PAID    → SHIPPED (отправлен)                                   ║
║  • PAID    → CANCELLED (возврат)                                   ║
║  • PAID    → FAILED (ошибка)                                       ║
║  • SHIPPED → DELIVERED (доставлен)                                 ║
║  • SHIPPED → CANCELLED (возврат)                                   ║
║  • FAILED  → CANCELLED (компенсация)                               ║
╚════════════════════════════════════════════════════════════════════╝";
    }
}

// =====================================================
// STOCK SERVICE (Проверка остатков)
// =====================================================

public class StockService
{
    private readonly Dictionary<int, int> _stock = new()
    {
        { 1, 10 },
        { 2, 50 },
        { 3, 30 },
        { 4, 5 },
        { 999, 0 }
    };

    public async Task<Result> CheckStockAsync(int productId, int quantity)
    {
        await Task.Delay(100);
        
        if (!_stock.ContainsKey(productId))
            return Result.Fail("Товар не найден");
        
        if (_stock[productId] >= quantity)
            return Result.Ok($"В наличии: {_stock[productId]} шт");
        
        return Result.Fail($"Запрошено: {quantity}, доступно: {_stock[productId]}");
    }
    
    public async Task<Result> ReserveStockAsync(int productId, int quantity)
    {
        await Task.Delay(100);
        
        if (!_stock.ContainsKey(productId))
            return Result.Fail("Товар не найден");
        
        if (_stock[productId] >= quantity)
        {
            _stock[productId] -= quantity;
            return Result.Ok($"Зарезервировано. Остаток: {_stock[productId]}");
        }
        
        return Result.Fail("Недостаточно товара");
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

// =====================================================
// SAGA ORCHESTRATOR
// =====================================================

public class SagaOrchestrator
{
    private readonly StockService _stockService;
    private readonly AsyncRetryPolicy _retryPolicy;
    private int _orderIdCounter = 1;

    public SagaOrchestrator()
    {
        _stockService = new StockService();
        
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(retryAttempt),
                onRetry: (ex, timeSpan, retryCount, ctx) =>
                {
                    Console.WriteLine($"      ⚠️ Повтор {retryCount} через {timeSpan.Seconds} сек");
                });
    }

    private async Task<Result> ProcessPaymentAsync(Order order)
    {
        await Task.Delay(300);
        if (order.TotalAmount <= 100000)
            return Result.Ok($"Оплата {order.TotalAmount} руб одобрена");
        return Result.Fail("Превышен лимит оплаты");
    }

    private async Task<Result> RefundPaymentAsync(Order order)
    {
        await Task.Delay(200);
        Console.WriteLine($"      💸 Возврат {order.TotalAmount} руб");
        return Result.Ok();
    }

    private async Task<Result> ShipOrderAsync(Order order)
    {
        await Task.Delay(300);
        return Result.Ok($"Трек-номер: TRK{order.Id}");
    }

    private async Task<Result> CancelShippingAsync(Order order)
    {
        await Task.Delay(200);
        Console.WriteLine($"      📦 Отмена отправки заказа #{order.Id}");
        return Result.Ok();
    }

    public async Task<Result> ExecuteOrderWorkflow(int customerId, List<OrderItem> items)
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(@"
╔════════════════════════════════════════════════════════════════════╗
║     🚀 СКВОЗНОЙ СЦЕНАРИЙ «ОФОРМЛЕНИЕ ЗАКАЗА» (End-to-End)        ║
║     BPMN: NEW → PAID → SHIPPED → DELIVERED + Saga Pattern         ║
╚════════════════════════════════════════════════════════════════════╝");
        Console.ResetColor();

        var order = new Order
        {
            Id = _orderIdCounter++,
            CustomerId = customerId,
            Items = items,
            CreatedAt = DateTime.Now,
            Status = OrderStatus.New
        };

        var stateMachine = new OrderStateMachine(order);
        var compensationStack = new Stack<Func<Task>>();

        Console.WriteLine($"\n📦 ЗАКАЗ #{order.Id}");
        Console.WriteLine($"   Сумма: {order.TotalAmount:N0} руб");
        Console.WriteLine($"   Товары: {string.Join(", ", items.Select(i => $"{i.ProductName} x{i.Quantity}"))}");
        Console.WriteLine($"\n{new string('═', 70)}");

        try
        {
            // ШАГ 1: Оплата
            Console.WriteLine("\n📌 ШАГ 1/4: Обработка оплаты...");
            await _retryPolicy.ExecuteAsync(async () =>
            {
                var result = await ProcessPaymentAsync(order);
                if (!result.Success) throw new Exception(result.Message);
            });
            stateMachine.TransitionTo(OrderStatus.Paid, "Оплата успешна");
            compensationStack.Push(async () => await RefundPaymentAsync(order));

            // ШАГ 2: Проверка остатков
            Console.WriteLine("\n📌 ШАГ 2/4: Проверка остатков...");
            foreach (var item in order.Items)
            {
                var stockResult = await _stockService.CheckStockAsync(item.ProductId, item.Quantity);
                if (!stockResult.Success)
                    throw new Exception($"{item.ProductName}: {stockResult.Message}");
                Console.WriteLine($"      ✅ {item.ProductName}: {stockResult.Message}");
                
                await _stockService.ReserveStockAsync(item.ProductId, item.Quantity);
            }
            compensationStack.Push(async () =>
            {
                foreach (var item in order.Items)
                    await _stockService.ReleaseStockAsync(item.ProductId, item.Quantity);
            });

            // ШАГ 3: Отправка
            Console.WriteLine("\n📌 ШАГ 3/4: Отправка заказа...");
            var shippingResult = await ShipOrderAsync(order);
            stateMachine.TransitionTo(OrderStatus.Shipped, shippingResult.Message);
            compensationStack.Push(async () => await CancelShippingAsync(order));

            // ШАГ 4: Доставка
            Console.WriteLine("\n📌 ШАГ 4/4: Подтверждение доставки...");
            await Task.Delay(500);
            stateMachine.TransitionTo(OrderStatus.Delivered, "Заказ доставлен клиенту");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n{new string('═', 70)}");
            Console.WriteLine("🎉 СКВОЗНОЙ СЦЕНАРИЙ УСПЕШНО ЗАВЕРШЕН!");
            Console.ResetColor();

            return Result.Ok($"Заказ #{order.Id} успешно оформлен и доставлен!");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n💥 ОШИБКА: {ex.Message}");
            Console.ResetColor();
            
            Console.WriteLine("\n🔄 ЗАПУСК КОМПЕНСАЦИИ (Saga Pattern)...");
            stateMachine.TransitionTo(OrderStatus.Failed, ex.Message);
            
            foreach (var compensate in compensationStack)
            {
                try
                {
                    await compensate();
                    Console.WriteLine("      ✅ Компенсация выполнена");
                }
                catch (Exception compEx)
                {
                    Console.WriteLine($"      ❌ Ошибка компенсации: {compEx.Message}");
                }
            }
            
            stateMachine.TransitionTo(OrderStatus.Cancelled, "Заказ отменен");
            
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n❌ ЗАКАЗ #{order.Id} ОТМЕНЕН");
            Console.ResetColor();
            
            return Result.Fail($"Ошибка: {ex.Message}");
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
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(@"
╔════════════════════════════════════════════════════════════════════╗
║     CRM СКВОЗНОЙ СЦЕНАРИЙ - ОФОРМЛЕНИЕ ЗАКАЗА (End-to-End)       ║
║                    Saga Pattern + State Machine                    ║
╚════════════════════════════════════════════════════════════════════╝");
        Console.ResetColor();

        var demoOrder = new Order { Id = 0, CustomerId = 0 };
        var demoSM = new OrderStateMachine(demoOrder);
        Console.WriteLine(demoSM.GetDiagram());

        Console.WriteLine("\n╔═══════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                    ЗАПУСК ТЕСТОВЫХ СЦЕНАРИЕВ                      ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════════════╝");

        var saga = new SagaOrchestrator();

        // ТЕСТ 1: УСПЕХ
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n\n🔹 ТЕСТ 1: Успешное оформление заказа");
        Console.WriteLine(new string('=', 60));
        Console.ResetColor();

        var successItems = new List<OrderItem>
        {
            new() { ProductId = 1, ProductName = "Ноутбук", Quantity = 1, Price = 50000 },
            new() { ProductId = 2, ProductName = "Мышь", Quantity = 2, Price = 1000 },
            new() { ProductId = 3, ProductName = "Клавиатура", Quantity = 1, Price = 3000 }
        };

        var result1 = await saga.ExecuteOrderWorkflow(1, successItems);

        // ТЕСТ 2: ОШИБКА
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("\n\n\n🔹 ТЕСТ 2: Сценарий с ошибкой (компенсация)");
        Console.WriteLine(new string('=', 60));
        Console.ResetColor();

        var failingItems = new List<OrderItem>
        {
            new() { ProductId = 999, ProductName = "Товар-которого-нет", Quantity = 1, Price = 100000 }
        };

        var result2 = await saga.ExecuteOrderWorkflow(2, failingItems);

        // ИТОГИ
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n\n╔═══════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                         ИТОГИ ТЕСТИРОВАНИЯ                         ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════════════╝");
        Console.ResetColor();

        Console.WriteLine($"\n📊 Тест 1 (успешный): {(result1.Success ? "✅ ПРОЙДЕН" : "❌ ПРОВАЛЕН")}");
        Console.WriteLine($"   {result1.Message}");

        Console.WriteLine($"\n📊 Тест 2 (компенсация): {(result2.Success ? "✅ ПРОЙДЕН" : "✅ КОМПЕНСАЦИЯ СРАБОТАЛА")}");
        Console.WriteLine($"   {result2.Message}");

        Console.WriteLine("\nНажми Enter для выхода...");
        Console.ReadLine();
    }
}