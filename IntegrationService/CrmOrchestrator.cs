using AutoMapper;
using IntegrationService.Adapters;
using IntegrationService.Models;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace IntegrationService;

public class CrmOrchestrator : ICrmOrchestrator
{
    private readonly ILeadsAdapter _leadsAdapter;
    private readonly ICartAdapter _cartAdapter;
    private readonly IDealsAdapter _dealsAdapter;
    private readonly ITasksAdapter _tasksAdapter;
    private readonly IReportsAdapter _reportsAdapter;
    private readonly IMapper _mapper;
    private readonly ILogger<CrmOrchestrator> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;

    public CrmOrchestrator(
        ILeadsAdapter leadsAdapter,
        ICartAdapter cartAdapter,
        IDealsAdapter dealsAdapter,
        ITasksAdapter tasksAdapter,
        IReportsAdapter reportsAdapter,
        IMapper mapper,
        ILogger<CrmOrchestrator> logger)
    {
        _leadsAdapter = leadsAdapter;
        _cartAdapter = cartAdapter;
        _dealsAdapter = dealsAdapter;
        _tasksAdapter = tasksAdapter;
        _reportsAdapter = reportsAdapter;
        _mapper = mapper;
        _logger = logger;
        
        // Политика повторных попыток (Retry)
        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(exception, "⚠️ Попытка {RetryCount} через {Delay} сек", retryCount, timeSpan.TotalSeconds);
                });
    }

    /// <summary>
    /// Основной бизнес-процесс: Корзина → Сделка → Задача → Отчет
    /// </summary>
    public async Task<OrderResult> ProcessOrderFromCartAsync(int leadId)
    {
        _logger.LogInformation("🚀 НАЧАЛО ИНТЕГРАЦИИ: Обработка заказа для лида #{LeadId}", leadId);
        
        var result = new OrderResult();
        
        try
        {
            // ========== ШАГ 1: Получаем лида ==========
            _logger.LogInformation("📌 ШАГ 1/5: Получение информации о лиде...");
            var lead = await _retryPolicy.ExecuteAsync(() => _leadsAdapter.GetLeadByIdAsync(leadId));
            if (lead == null)
            {
                throw new Exception($"Лид #{leadId} не найден");
            }
            _logger.LogInformation("✅ Лид найден: {LeadName} ({LeadPhone})", lead.Name, lead.Phone);
            
            // ========== ШАГ 2: Получаем корзину ==========
            _logger.LogInformation("📌 ШАГ 2/5: Получение содержимого корзины...");
            var cart = await _retryPolicy.ExecuteAsync(() => _cartAdapter.GetCartAsync());
            if (cart.Items == null || !cart.Items.Any())
            {
                throw new Exception("Корзина пуста");
            }
            _logger.LogInformation("✅ В корзине {Count} товаров на сумму {Total} руб", 
                cart.Items.Count, cart.Total);
            
            // ========== ШАГ 3: МАППИНГ: Корзина → Сделка ==========
            _logger.LogInformation("📌 ШАГ 3/5: Маппинг данных (Cart → Deal)...");
            var orderRequest = _mapper.Map<OrderRequest>(cart);
            orderRequest.LeadId = leadId;
            orderRequest.ContactId = leadId;
            
            var createDealRequest = _mapper.Map<CreateDealRequest>(orderRequest);
            _logger.LogInformation("✅ Создана заявка на сделку на сумму {Amount} руб", createDealRequest.Amount);
            
            // ========== ШАГ 4: Создаем сделку ==========
            _logger.LogInformation("📌 ШАГ 4/5: Создание сделки в модуле В...");
            var deal = await _retryPolicy.ExecuteAsync(() => _dealsAdapter.CreateDealAsync(createDealRequest));
            result.DealId = deal.Id;
            _logger.LogInformation("✅ Сделка #{DealId} создана, статус: {Stage}", deal.Id, deal.Stage);
            
            // ========== ШАГ 5: МАППИНГ: Сделка → Задача ==========
            _logger.LogInformation("📌 ШАГ 5/6: Маппинг данных (Deal → Task)...");
            var createTaskRequest = _mapper.Map<CreateTaskRequest>(deal);
            _logger.LogInformation("✅ Создана задача: {TaskTitle}", createTaskRequest.Title);
            
            // ========== ШАГ 6: Создаем задачу ==========
            _logger.LogInformation("📌 ШАГ 6/6: Создание задачи в модуле D...");
            var task = await _retryPolicy.ExecuteAsync(() => _tasksAdapter.CreateTaskAsync(createTaskRequest));
            result.TaskId = task.Id;
            _logger.LogInformation("✅ Задача #{TaskId} создана, исполнитель: {AssignedTo}", task.Id, task.AssignedTo);
            
            // ========== ФИНАЛ: Очищаем корзину ==========
            await _cartAdapter.ClearCartAsync();
            _logger.LogInformation("🗑️ Корзина очищена");
            
            // ========== Генерируем отчет ==========
            var report = await GenerateDailyReportAsync();
            result.ReportId = report.Id;
            
            result.Status = "success";
            result.Message = $"Заказ успешно обработан! Создана сделка #{deal.Id} и задача #{task.Id}";
            
            _logger.LogInformation("🎉 ИНТЕГРАЦИЯ ЗАВЕРШЕНА УСПЕШНО! Сделка #{DealId}, Задача #{TaskId}, Отчет #{ReportId}", 
                deal.Id, task.Id, report.Id);
        }
        catch (Exception ex)
        {
            result.Status = "error";
            result.Message = ex.Message;
            _logger.LogError(ex, "💥 ОШИБКА ИНТЕГРАЦИИ: {Message}", ex.Message);
        }
        
        return result;
    }

    /// <summary>
    /// Полный CRM workflow: Лид → Сделка → Задача → Отчет
    /// </summary>
    public async Task<OrderResult> ProcessFullCrmWorkflowAsync(CreateLeadRequest leadRequest)
    {
        _logger.LogInformation("🚀 ПОЛНЫЙ CRM WORKFLOW");
        
        // 1. Создаем лида
        var lead = await _leadsAdapter.CreateLeadAsync(leadRequest);
        _logger.LogInformation("✅ Создан лид #{LeadId}: {LeadName}", lead.Id, lead.Name);
        
        // 2. Добавляем тестовый товар в корзину
        await _cartAdapter.AddToCartAsync(new CartItemDto 
        { 
            Id = 1, 
            Name = "Ноутбук", 
            Price = 50000, 
            Quantity = 1 
        });
        
        // 3. Обрабатываем заказ
        var result = await ProcessOrderFromCartAsync(lead.Id);
        
        return result;
    }

    /// <summary>
    /// Генерация дневного отчета
    /// </summary>
    public async Task<ReportDto> GenerateDailyReportAsync()
    {
        var deals = await _dealsAdapter.GetAllDealsAsync();
        var total = deals.Sum(d => d.Amount);
        
        var reportRequest = new CreateReportRequest
        {
            Period = "daily",
            Total = total,
            DealsCount = deals.Count
        };
        
        var report = await _reportsAdapter.CreateSalesReportAsync(reportRequest);
        _logger.LogInformation("📊 Создан отчет: выручка {Total} руб, сделок {Count}", total, deals.Count);
        
        return report;
    }

    /// <summary>
    /// Статус системы
    /// </summary>
    public async Task<string> GetSystemStatusAsync()
    {
        var status = new Dictionary<string, object>();
        
        try
        {
            var leads = await _leadsAdapter.GetAllLeadsAsync();
            status["leads"] = new { status = "ok", count = leads.Count };
        }
        catch { status["leads"] = new { status = "error" }; }
        
        try
        {
            var cart = await _cartAdapter.GetCartAsync();
            status["cart"] = new { status = "ok", items = cart.Items?.Count ?? 0 };
        }
        catch { status["cart"] = new { status = "error" }; }
        
        try
        {
            var deals = await _dealsAdapter.GetAllDealsAsync();
            status["deals"] = new { status = "ok", count = deals?.Count ?? 0 };
        }
        catch { status["deals"] = new { status = "error" }; }
        
        try
        {
            var tasks = await _tasksAdapter.GetAllTasksAsync();
            status["tasks"] = new { status = "ok", count = tasks?.Count ?? 0 };
        }
        catch { status["tasks"] = new { status = "error" }; }
        
        try
        {
            var reports = await _reportsAdapter.GetAllReportsAsync();
            status["reports"] = new { status = "ok", count = reports?.Count ?? 0 };
        }
        catch { status["reports"] = new { status = "error" }; }
        
        return System.Text.Json.JsonSerializer.Serialize(status, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }
}