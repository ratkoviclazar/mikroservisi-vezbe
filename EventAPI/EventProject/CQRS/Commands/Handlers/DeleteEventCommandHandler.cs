using EventAPI.CQRS.Abstractions;
using EventAPI.CQRS.Commands.Validation;
using EventAPI.CQRS.DataAccess;
using EventAPI.Data;
using EventAPI.EventSourcing.Services;

namespace EventAPI.CQRS.Commands.Handlers
{
    public sealed class DeleteEventCommandHandler : ICommandHandler<DeleteEventCommand, CommandResult>
    {
        private readonly IEventsWriteStore _writeStore;
        private readonly ICommandValidator<DeleteEventCommand> _validator;
        private readonly IEventSourcingService _eventSourcingService;
        private readonly EventsDbContext _db;
        public DeleteEventCommandHandler(
            IEventsWriteStore writeStore,
            ICommandValidator<DeleteEventCommand> validator,
            IEventSourcingService eventSourcingService,
            EventsDbContext db)
        {
            _writeStore = writeStore;
            _validator = validator;
            _eventSourcingService = eventSourcingService;
            _db = db;
        }

        public async Task<CommandResult> HandleAsync(DeleteEventCommand command, CancellationToken cancellationToken = default)
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

                aggregate.Delete();

                var deleted = await _writeStore.DeleteEventAsync(command.Id, cancellationToken);

                if (!deleted)
                    return CommandResult.NotFound($"Događaj sa Id {command.Id} ne postoji.");

                await _eventSourcingService.SaveAsync(aggregate, cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                return CommandResult.Ok();
            }
            catch (Exception ex)
            {
                return CommandResult.Error($"Greška prilikom brisanja događaja: {ex.Message}");
            }
        }
    }
}
