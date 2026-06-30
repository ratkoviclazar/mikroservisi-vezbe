using EventAPI.CQRS.Abstractions;
using EventAPI.CQRS.Commands.Validation;
using EventAPI.CQRS.DataAccess;
using EventAPI.Data;
using EventAPI.Domains;
using EventAPI.EventSourcing.Aggregates;
using EventAPI.EventSourcing.Services;

namespace EventAPI.CQRS.Commands.Handlers
{
    public sealed class CreateEventCommandHandler : ICommandHandler<CreateEventCommand, CommandResult<int>>
    {
        private readonly IEventsWriteStore _writeStore;
        private readonly ICommandValidator<CreateEventCommand> _validator;
        private readonly IEventSourcingService _eventSourcingService;
        private readonly EventsDbContext _db;

        public CreateEventCommandHandler(
            IEventsWriteStore writeStore,
            ICommandValidator<CreateEventCommand> validator,
            IEventSourcingService eventSourcingService,
            EventsDbContext db)
        {
            _writeStore = writeStore;
            _validator = validator;
            _eventSourcingService = eventSourcingService;
            _db = db;
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
                await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

                var newId = await _writeStore.CreateEventAsync(entity, cancellationToken);

                var aggregate = EventAggregate.Create(
                    id: newId,
                    name: command.Name,
                    agenda: command.Agenda,
                    dateTime: command.DateTime,
                    durationInHours: command.DurationInHours,
                    price: command.Price,
                    typeId: command.TypeId,
                    locationId: command.LocationId);

                await _eventSourcingService.SaveAsync(aggregate, cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                return CommandResult<int>.Ok(newId);
            }
            catch (Exception ex)
            {
                return CommandResult<int>.Error($"Greška prilikom kreiranja događaja: {ex.Message}");
            }
        }
    }
}
