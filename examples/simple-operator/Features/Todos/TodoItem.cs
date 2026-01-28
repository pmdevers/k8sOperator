using k8s.Models;
using k8s.Operator.Metadata;
using k8s.Operator.Models;

namespace simple_operator.Features.Todos;

[KubernetesEntity(Group = "app.example.com", ApiVersion = "v1", Kind = "TodoItem", PluralName = "todoitems")]
[AdditionalPrinterColumn(Name = "Title", Description = "Todo Title", Path = ".spec.title", Type = "string")]
[AdditionalPrinterColumn(Name = "Description", Description = "Todo Description", Path = ".spec.description", Type = "string")]
[AdditionalPrinterColumn(Name = "State", Description = "Todo State", Path = ".status.state", Type = "string")]
public class TodoItem : CustomResource<TodoItem.TodoSpec, TodoItem.TodoStatus>
{
    public class TodoSpec
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Priority { get; set; } = "medium"; // low, medium, high
        public DateTime? DueDate { get; set; }
    }

    public class TodoStatus
    {
        public string State { get; set; } = "pending"; // pending, in-progress, completed
        public DateTime? CompletedAt { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ReconciliationCount { get; set; }
    }
}

