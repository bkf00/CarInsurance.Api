namespace CarInsurance.Api.Models
{
    public class HistoryEvent
    {
        public DateOnly EventDate { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

    }
}
