namespace EventAPI.Domain.Events
{
    public sealed class EventPriceChanged : DomainEvent
    {
        public decimal Price { get; set; }
    }

}
