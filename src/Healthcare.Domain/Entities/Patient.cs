namespace Healthcare.Domain.Entities;

public class Patient
{
    public Guid Id { get; set; }
    public ICollection<UsageEvent> UsageEvents { get; set; } = [];
}
