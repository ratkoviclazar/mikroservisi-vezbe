using EventAPI.CQRS.Abstractions;
using EventAPI.CQRS.Commands.Validation;
using EventAPI.CQRS.DataAccess;

namespace EventAPI.CQRS.Commands.Handlers
{
    public sealed class DeleteEventCommandHandler : ICommandHandler<DeleteEventCommand, CommandResult>
    {
        private readonly IEventsWriteStore _writeStore;
        private readonly ICommandValidator<DeleteEventCommand> _validator;

        public DeleteEventCommandHandler(IEventsWriteStore writeStore, ICommandValidator<DeleteEventCommand> validator)
        {
            _writeStore = writeStore;
            _validator = validator;
        }

        public async Task<CommandResult> HandleAsync(DeleteEventCommand command, CancellationToken cancellationToken = default)
        {
            var errors = await _validator.ValidateAsync(command, cancellationToken);

            if (errors.Count > 0)
                return CommandResult.ValidationFailed(errors);

            try
            {
                var deleted = await _writeStore.DeleteEventAsync(command.Id, cancellationToken);

                if (!deleted)
                    return CommandResult.NotFound($"Događaj sa Id {command.Id} ne postoji.");

                return CommandResult.Ok();
            }
            catch (System.Exception ex)
            {
                return CommandResult.Error($"Greška prilikom brisanja događaja: {ex.Message}");
            }
        }
    }
}
