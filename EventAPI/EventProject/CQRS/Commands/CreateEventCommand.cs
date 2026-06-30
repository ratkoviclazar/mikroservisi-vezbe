using EventAPI.CQRS.Abstractions;

namespace EventAPI.CQRS.Commands
{
    public sealed class CreateEventCommand : ICommand<CommandResult<int>>
    {
        public string Name { get; init; } = string.Empty;
        public string Agenda { get; init; } = string.Empty;
        public DateTime DateTime { get; init; }
        public decimal DurationInHours { get; init; }
        public decimal Price { get; init; }
        public int TypeId { get; init; }
        public int LocationId { get; init; }
    }
}
