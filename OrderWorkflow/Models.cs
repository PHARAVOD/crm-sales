namespace OrderWorkflow;

// Статусы заказа для State Machine
public enum OrderStatus
{
    New,        // Новый заказ
    Paid,       // Оплачен
    Shipped,    // Отправлен
    Delivered,  // Доставлен
    Cancelled,  // Отменен
    Failed      // Ошибка
}

// Товар в заказе
public class OrderItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Total => Quantity * Price;
}

// Заказ
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

// Результат операции
public class Result
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public object? Data { get; set; }
    
    public static Result Ok(string msg = "Успешно") => new() { Success = true, Message = msg };
    public static Result Fail(string msg) => new() { Success = false, Message = msg };
}