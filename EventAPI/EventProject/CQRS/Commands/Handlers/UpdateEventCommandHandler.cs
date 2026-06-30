using EventAPI.CQRS.Abstractions;
using EventAPI.CQRS.Commands.Validation;
using EventAPI.CQRS.DataAccess;
using EventAPI.Data;
using EventAPI.EventSourcing.Services;

namespace EventAPI.CQRS.Commands.Handlers
{
    public sealed class UpdateEventCommandHandler : ICommandHandler<UpdateEventCommand, CommandResult>
    {
        private readonly IEventsWriteStore _writeStore;
        private readonly ICommandValidator<UpdateEventCommand> _validator;
        private readonly IEventSourcingService _eventSourcingService;
        private readonly EventsDbContext _db;
        public UpdateEventCommandHandler(
            IEventsWriteStore writeStore,
            ICommandValidator<UpdateEventCommand> validator,
            IEventSourcingService eventSourcingService,
            EventsDbContext db)
        {
            _writeStore = writeStore;
            _validator = validator;
            _eventSourcingService = eventSourcingService;
            _db = db;
        }

        public async Task<CommandResult> HandleAsync(UpdateEventCommand command, CancellationToken cancellationToken = default)
        {
            var errors = await _validator.ValidateAsync(command, cancellationToken);

            if (errors.Count > 0)
                return CommandResult.ValidationFailed(errors);

            try
            {
                await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

                var aggregate = await _eventSourcingService.GetByIdAsync(command.Id, cancellationToken);

                if (aggregate is null)
                    return CommandResult.NotFound($"Događaj sa Id {command.Id} ne postoji.");

                aggregate.ChangeName(command.Name);
                aggregate.ChangeAgenda(command.Agenda);
                aggregate.ChangeDateTime(command.DateTime);
                aggregate.ChangeDuration(command.DurationInHours);
                aggregate.ChangePrice(command.Price);
                aggregate.ChangeType(command.TypeId);
                aggregate.ChangeLocation(command.LocationId);

                var updated = await _writeStore.UpdateEventAsync(command.Id, entity =>
                {
                    entity.Name = aggregate.Name;
                    entity.Agenda = aggregate.Agenda;
                    entity.DateTime = aggregate.DateTime;
                    entity.DurationInHours = aggregate.DurationInHours;
                    entity.Price = aggregate.Price;
                    entity.TypeId = aggregate.TypeId;
                    entity.LocationId = aggregate.LocationId;
                }, cancellationToken);

                if (!updated)
                    return CommandResult.NotFound($"Događaj sa Id {command.Id} ne postoji.");

                await _eventSourcingService.SaveAsync(aggregate, cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                return CommandResult.Ok();
            }
            catch (System.Exception ex)
            {
                return CommandResult.Error($"Greška prilikom izmene događaja: {ex.Message}");
            }
        }
    }
}
