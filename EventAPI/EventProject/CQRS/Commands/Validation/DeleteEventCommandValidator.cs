namespace EventAPI.CQRS.Commands.Validation
{

    public sealed class DeleteEventCommandValidator : ICommandValidator<DeleteEventCommand>
    {
        public Task<IReadOnlyList<string>> ValidateAsync(DeleteEventCommand command, CancellationToken cancellationToken = default)
        {
            var errors = new List<string>();

            if (command.Id <= 0)
                errors.Add("Id događaja nije validan.");

            return Task.FromResult<IReadOnlyList<string>>(errors);
        }
    }
}
