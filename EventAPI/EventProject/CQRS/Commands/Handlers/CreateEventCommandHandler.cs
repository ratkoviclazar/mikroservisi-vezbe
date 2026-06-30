using EventAPI.CQRS.Abstractions;
using EventAPI.CQRS.Commands.Validation;
using EventAPI.CQRS.DataAccess;
using EventAPI.Domains;

namespace EventAPI.CQRS.Commands.Handlers
{
    public sealed class CreateEventCommandHandler : ICommandHandler<CreateEventCommand, CommandResult<int>>
    {
        private readonly IEventsWriteStore _writeStore;
        private readonly ICommandValidator<CreateEventCommand> _validator;

        public CreateEventCommandHandler(IEventsWriteStore writeStore, ICommandValidator<CreateEventCommand> validator)
        {
            _writeStore = writeStore;
            _validator = validator;
        }

        public async Task<CommandResult<int>> HandleAsync(CreateEventCommand command, CancellationToken cancellationToken = default)
        {
            var errors = await _validator.ValidateAsync(command, cancellationToken);

            if (errors.Count > 0)
                return CommandResult<int>.ValidationFailed(errors);

            var entity = new Event
            {
                Name = command.Name,
                Agenda = command.Agenda,
                DateTime = command.DateTime,
                DurationInHours = command.DurationInHours,
                Price = command.Price,
                TypeId = command.TypeId,
                LocationId = command.LocationId
            };

            try
            {
                var newId = await _writeStore.CreateEventAsync(entity, cancellationToken);
                return CommandResult<int>.Ok(newId);
            }
            catch (System.Exception ex)
            {
                return CommandResult<int>.Error($"Greška prilikom kreiranja događaja: {ex.Message}");
            }
        }
    }
}
