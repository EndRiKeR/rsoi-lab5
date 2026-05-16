namespace StatisticService.Database.Models;

public class StatisticsEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } // "TicketPurchased", "TicketReturned", "BonusUpdated"
    public DateTime Timestamp { get; set; }
    public string Username { get; set; }
    public string Payload { get; set; } // JSON-строка с деталями
}