using EventAPI.CQRS.Abstractions;

namespace EventAPI.CQRS.Commands
{
    public sealed class DeleteEventCommand : ICommand<CommandResult>
    {
        public int Id { get; init; }
    }
}
