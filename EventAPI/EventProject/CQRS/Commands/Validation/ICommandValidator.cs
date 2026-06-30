namespace EventAPI.CQRS.Commands.Validation
{

    public interface ICommandValidator<TCommand>
    {
        Task<IReadOnlyList<string>> ValidateAsync(TCommand command, CancellationToken cancellationToken = default);
    }
}
