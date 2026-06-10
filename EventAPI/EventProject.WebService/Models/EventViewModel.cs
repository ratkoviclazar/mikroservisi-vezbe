public class EventViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Agenda { get; set; } = string.Empty;
    public DateTime DateTime { get; set; }
    public decimal DurationInHours { get; set; }
    public decimal Price { get; set; }
    public int TypeId { get; set; }
    public string? TypeName { get; set; }
    public int LocationId { get; set; }
    public string? LocationName { get; set; }
}
