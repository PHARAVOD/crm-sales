using IntegrationService;
using IntegrationService.Adapters;
using IntegrationService.Mappings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

class Program
{
    static async Task Main(string[] args)
    {
        // Настройка Serilog для логирования
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File("logs/integration-service-.txt", 
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}")
            .CreateLogger();

        try
        {
            Log.Information("========================================");
            Log.Information("🚀 ЗАПУСК ИНТЕГРАЦИОННОГО СЕРВИСА CRM");
            Log.Information("========================================");
            
            // Настройка DI контейнера
            var services = new ServiceCollection();
            
            // Добавляем HttpClient
            services.AddHttpClient();
            
            // Добавляем адаптеры
            services.AddScoped<ILeadsAdapter, LeadsAdapter>();
            services.AddScoped<ICartAdapter, CartAdapter>();
            services.AddScoped<IDealsAdapter, DealsAdapter>();
            services.AddScoped<ITasksAdapter, TasksAdapter>();
            services.AddScoped<IReportsAdapter, ReportsAdapter>();
            
            // Добавляем оркестратор
            services.AddScoped<ICrmOrchestrator, CrmOrchestrator>();
            
            // Добавляем AutoMapper
            services.AddAutoMapper(typeof(MappingProfile));
            
            // Добавляем логирование
            services.AddLogging(builder =>
            {
                builder.AddSerilog(dispose: true);
            });
            
            var serviceProvider = services.BuildServiceProvider();
            var orchestrator = serviceProvider.GetRequiredService<ICrmOrchestrator>();
            
            // ========== ДЕМОНСТРАЦИЯ РАБОТЫ ==========
            
            Log.Information("\n📋 ПРОВЕРКА СТАТУСА СИСТЕМЫ");
            Log.Information("----------------------------------------");
            var status = await orchestrator.GetSystemStatusAsync();
            Log.Information("Статус:\n{Status}", status);
            
            Log.Information("\n🔄 ЗАПУСК ОСНОВНОГО БИЗНЕС-ПРОЦЕССА");
            Log.Information("----------------------------------------");
            
            // Создаем тестового лида
            var newLead = new IntegrationService.Models.CreateLeadRequest
            {
                Name = "Тестовый клиент",
                Phone = "+79991234567",
                Email = "test@example.com",
                Source = "integration_service"
            };
            
            Log.Information("👤 Создание тестового лида: {Name}", newLead.Name);
            
            // Запускаем полный workflow
            var result = await orchestrator.ProcessFullCrmWorkflowAsync(newLead);
            
            Log.Information("\n📊 РЕЗУЛЬТАТ ИНТЕГРАЦИИ");
            Log.Information("----------------------------------------");
            Log.Information("Статус: {Status}", result.Status);
            Log.Information("Сообщение: {Message}", result.Message);
            Log.Information("Сделка ID: {DealId}", result.DealId);
            Log.Information("Задача ID: {TaskId}", result.TaskId);
            Log.Information("Отчет ID: {ReportId}", result.ReportId);
            
            Log.Information("\n✅ ИНТЕГРАЦИОННЫЙ СЕРВИС УСПЕШНО ЗАВЕРШИЛ РАБОТУ");
            
            // Генерируем дополнительный отчет
            Log.Information("\n📈 ГЕНЕРАЦИЯ ДНЕВНОГО ОТЧЕТА");
            var report = await orchestrator.GenerateDailyReportAsync();
            Log.Information("Отчет #{ReportId}: выручка {TotalSales} руб, сделок {DealsCount}", 
                report.Id, report.TotalSales, report.DealsCount);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "💥 КРИТИЧЕСКАЯ ОШИБКА: {Message}", ex.Message);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}