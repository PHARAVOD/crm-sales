namespace OrderWorkflow;

public class OrderStateMachine
{
    private Order _order;
    
    // Допустимые переходы между статусами
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
        _order.AddEvent($"StateMachine инициализирована. Текущий статус: {_order.Status}");
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
            _order.AddEvent($"❌ НЕВОЗМОЖНО: {_order.Status} → {newStatus}");
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
╔══════════════════════════════════════════════════════════════════════════════╗
║                         UML STATE MACHINE DIAGRAM                            ║
║                           Статусы заказа (Order)                              ║
╠══════════════════════════════════════════════════════════════════════════════╣
║                                                                              ║
║                              ┌─────────────┐                                 ║
║                              │    START    │                                 ║
║                              └──────┬──────┘                                 ║
║                                     │                                        ║
║                                     ▼                                        ║
║   ┌─────────────────────────────────────────────────────────────────────┐   ║
║   │                        ┌─────────────┐                              │   ║
║   │                    ┌──▶│    NEW      │                              │   ║
║   │                    │   └──────┬──────┘                              │   ║
║   │                    │          │                                     │   ║
║   │              Отмена│          │ Оплата                              │   ║
║   │                    │          ▼                                     │   ║
║   │                    │   ┌─────────────┐     ┌─────────────┐          │   ║
║   │                    └──▶│    PAID     │────▶│   FAILED   │          │   ║
║   │                        └──────┬──────┘     └──────┬──────┘          │   ║
║   │                               │                   │                 │   ║
║   │                         Отправка                  │ Отмена          │   ║
║   │                               │                   │                 │   ║
║   │                               ▼                   ▼                 │   ║
║   │                        ┌─────────────┐     ┌─────────────┐          │   ║
║   │                        │   SHIPPED   │────▶│  CANCELLED  │          │   ║
║   │                        └──────┬──────┘     └─────────────┘          │   ║
║   │                               │                                     │   ║
║   │                         Доставка                                    │   ║
║   │                               ▼                                     │   ║
║   │                        ┌─────────────┐                              │   ║
║   │                        │  DELIVERED  │                              │   ║
║   │                        └─────────────┘                              │   ║
║   └─────────────────────────────────────────────────────────────────────┘   ║
║                                                                              ║
╠══════════════════════════════════════════════════════════════════════════════╣
║  Допустимые переходы:                                                         ║
║  • NEW      → PAID (оплата успешна)                                          ║
║  • NEW      → CANCELLED (отмена заказа)                                      ║
║  • PAID     → SHIPPED (заказ отправлен)                                      ║
║  • PAID     → CANCELLED (возврат средств)                                    ║
║  • PAID     → FAILED (ошибка оплаты)                                         ║
║  • SHIPPED  → DELIVERED (заказ доставлен)                                    ║
║  • SHIPPED  → CANCELLED (возврат товара)                                     ║
║  • FAILED   → CANCELLED (компенсация)                                        ║
╚══════════════════════════════════════════════════════════════════════════════╝";
    }
}