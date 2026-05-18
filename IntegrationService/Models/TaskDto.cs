namespace IntegrationService.Models;

public class TaskDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public int DealId { get; set; }
    public string AssignedTo { get; set; }
    public string DueDate { get; set; }
    public string Status { get; set; }
}

public class CreateTaskRequest
{
    public string Title { get; set; }
    public string Description { get; set; }
    public int DealId { get; set; }
    public string AssignedTo { get; set; }
    public string DueDate { get; set; }
}