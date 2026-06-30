using EventAPI.CQRS.Abstractions;
using EventAPI.CQRS.Commands.Validation;
using EventAPI.CQRS.DataAccess;

namespace EventAPI.CQRS.Commands.Handlers
{
    public sealed class UpdateEventCommandHandler : ICommandHandler<UpdateEventCommand, CommandResult>
    {
        private readonly IEventsWriteStore _writeStore;
        private readonly ICommandValidator<UpdateEventCommand> _validator;

        public UpdateEventCommandHandler(IEventsWriteStore writeStore, ICommandValidator<UpdateEventCommand> validator)
        {
            _writeStore = writeStore;
            _validator = validator;
        }

        public async Task<CommandResult> HandleAsync(UpdateEventCommand command, CancellationToken cancellationToken = default)
        {
            var errors = await _validator.ValidateAsync(command, cancellationToken);

            if (errors.Count > 0)
                return CommandResult.ValidationFailed(errors);

            try
            {
                var updated = await _writeStore.UpdateEventAsync(command.Id, entity =>
                {
                    entity.Name = command.Name;
                    entity.Agenda = command.Agenda;
                    entity.DateTime = command.DateTime;
                    entity.DurationInHours = command.DurationInHours;
                    entity.Price = command.Price;
                    entity.TypeId = command.TypeId;
                    entity.LocationId = command.LocationId;
                }, cancellationToken);

                if (!updated)
                    return CommandResult.NotFound($"Događaj sa Id {command.Id} ne postoji.");

                return CommandResult.Ok();
            }
            catch (System.Exception ex)
            {
                return CommandResult.Error($"Greška prilikom izmjene događaja: {ex.Message}");
            }
        }
    }
}
