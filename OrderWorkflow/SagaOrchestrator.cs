using Polly;
using Polly.Retry;

namespace OrderWorkflow;

public class SagaOrchestrator
{
    private readonly StockService _stockService;
    private readonly AsyncRetryPolicy _retryPolicy;
    private int _orderIdCounter = 1;

    public SagaOrchestrator()
    {
        _stockService = new StockService();
        
        // Политика повторных попыток (3 раза, с задержкой 1, 2, 4 секунды)
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 1)),
                onRetry: (ex, timeSpan, retryCount, ctx) =>
                {
                    Console.WriteLine($"      ⚠️ Повторная попытка {retryCount} через {timeSpan.Seconds} сек...");
                });
    }

    /// <summary>
    /// ГЛАВНЫЙ СКВОЗНОЙ СЦЕНАРИЙ: Оформление заказа
    /// End-to-End процесс согласно BPMN
    /// </summary>
    public async Task<Result> ExecuteEndToEndScenario(int customerId, List<OrderItem> items)
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(@"
╔══════════════════════════════════════════════════════════════════════════════╗
║                                                                              ║
║              🚀 СКВОЗНОЙ СЦЕНАРИЙ «ОФОРМЛЕНИЕ ЗАКАЗА» (End-to-End)          ║
║                                                                              ║
║              BPMN Process: NEW → PAID → SHIPPED → DELIVERED                  ║
║              + Saga Pattern + Проверка остатков + Компенсация                ║
║                                                                              ║
╚══════════════════════════════════════════════════════════════════════════════╝");
        Console.ResetColor();

        // Создаем заказ
        var order = new Order
        {
            Id = _orderIdCounter++,
            CustomerId = customerId,
            Items = items,
            CreatedAt = DateTime.Now,
            Status = OrderStatus.New
        };

        var stateMachine = new OrderStateMachine(order);
        
        // Стек для компенсаций (Saga Pattern)
        var compensationStack = new Stack<Func<Task>>();

        Console.WriteLine($"\n📦 ЗАКАЗ #{order.Id}");
        Console.WriteLine($"   👤 Клиент: {customerId}");
        Console.WriteLine($"   💰 Сумма: {order.TotalAmount:N0} руб");
        Console.WriteLine($"   🛒 Товары: {string.Join(", ", items.Select(i => $"{i.ProductName} x{i.Quantity}"))}");
        Console.WriteLine($"\n{new string('═', 70)}");

        try
        {
            // ========== ШАГ 1: NEW → PAID (Оплата) ==========
            Console.WriteLine("\n📌 ШАГ 1: Обработка оплаты...");
            await _retryPolicy.ExecuteAsync(async () =>
            {
                var paymentResult = await ProcessPaymentAsync(order);
                if (!paymentResult.Success)
                    throw new Exception(paymentResult.Message);
            });
            
            stateMachine.TransitionTo(OrderStatus.Paid, "Оплата успешно проведена");
            // Регистрируем компенсацию (откат оплаты)
            compensationStack.Push(async () => await RefundPaymentAsync(order));

            // ========== ШАГ 2: Проверка остатков (Stock Service) ==========
            Console.WriteLine("\n📌 ШАГ 2: Проверка остатков товаров...");
            
            foreach (var item in order.Items)
            {
                var stockResult = await _stockService.CheckStockAsync(item.ProductId, item.Quantity);
                if (!stockResult.Success)
                    throw new Exception($"{item.ProductName}: {stockResult.Message}");
                Console.WriteLine($"      ✅ {item.ProductName}: заказано {item.Quantity} шт - {stockResult.Message}");
            }
            
            // Резервируем товары
            foreach (var item in order.Items)
            {
                await _stockService.ReserveStockAsync(item.ProductId, item.Quantity);
                Console.WriteLine($"      📦 {item.ProductName}: зарезервировано {item.Quantity} шт");
            }
            compensationStack.Push(async () =>
            {
                foreach (var item in order.Items)
                    await _stockService.ReleaseStockAsync(item.ProductId, item.Quantity);
            });

            // ========== ШАГ 3: PAID → SHIPPED (Отправка) ==========
            Console.WriteLine("\n📌 ШАГ 3: Отправка заказа...");
            await _retryPolicy.ExecuteAsync(async () =>
            {
                var shippingResult = await ShipOrderAsync(order);
                if (!shippingResult.Success)
                    throw new Exception(shippingResult.Message);
            });
            
            stateMachine.TransitionTo(OrderStatus.Shipped, "Заказ передан в доставку");
            compensationStack.Push(async () => await CancelShippingAsync(order));

            // ========== ШАГ 4: SHIPPED → DELIVERED (Доставка) ==========
            Console.WriteLine("\n📌 ШАГ 4: Подтверждение доставки...");
            await Task.Delay(500);
            stateMachine.TransitionTo(OrderStatus.Delivered, "Заказ доставлен клиенту");

            // ========== ФИНАЛ ==========
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n{new string('═', 70)}");