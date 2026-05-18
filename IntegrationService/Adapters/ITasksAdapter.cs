using IntegrationService.Models;

namespace IntegrationService.Adapters;

public interface ITasksAdapter
{
    Task<List<TaskDto>> GetAllTasksAsync();
    Task<TaskDto> CreateTaskAsync(CreateTaskRequest request);
    Task<TaskDto> CompleteTaskAsync(int taskId);
}