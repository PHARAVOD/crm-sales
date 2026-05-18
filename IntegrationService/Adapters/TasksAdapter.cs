using System.Text;
using System.Text.Json;
using IntegrationService.Models;
using Microsoft.Extensions.Logging;

namespace IntegrationService.Adapters;

public class TasksAdapter : ITasksAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TasksAdapter> _logger;
    private const string BaseUrl = "http://localhost:5003";

    public TasksAdapter(HttpClient httpClient, ILogger<TasksAdapter> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<TaskDto>> GetAllTasksAsync()
    {
        var response = await _httpClient.GetAsync($"{BaseUrl}/tasks");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<TaskDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public async Task<TaskDto> CreateTaskAsync(CreateTaskRequest request)
    {
        try
        {
            _logger.LogInformation("📡 Вызов модуля D (Задачи): POST /tasks/create, задача={Title}", request.Title);
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{BaseUrl}/tasks/create", content);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var task = JsonSerializer.Deserialize<TaskDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            _logger.LogInformation("✅ Создана задача #{TaskId}: {Title}", task?.Id, task?.Title);
            return task;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Ошибка создания задачи");
            throw;
        }
    }

    public async Task<TaskDto> CompleteTaskAsync(int taskId)
    {
        var response = await _httpClient.PatchAsync($"{BaseUrl}/tasks/{taskId}/complete", null);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<TaskDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
}