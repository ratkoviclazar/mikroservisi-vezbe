using EventAPI.CQRS.DataAccess;

namespace EventAPI.CQRS.Commands.Validation
{

    public sealed class UpdateEventCommandValidator : ICommandValidator<UpdateEventCommand>
    {
        private readonly IEventsWriteStore _writeStore;

        public UpdateEventCommandValidator(IEventsWriteStore writeStore)
        {
            _writeStore = writeStore;
        }

        public async Task<IReadOnlyList<string>> ValidateAsync(UpdateEventCommand command, CancellationToken cancellationToken = default)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(command.Name))
                errors.Add("Naziv događaja je obavezan.");

            if (command.DurationInHours <= 0)
                errors.Add("Trajanje mora biti veće od 0.");

            if (command.Price < 0)
                errors.Add("Cena ne može biti negativna.");

            if (command.DateTime < DateTime.UtcNow)
                errors.Add("Datum ne sme biti u prošlosti.");

            if (!await _writeStore.LocationExistsAsync(command.LocationId, cancellationToken))
                errors.Add("Lokacija sa zadatim Id ne postoji.");

            if (!await _writeStore.EventTypeExistsAsync(command.TypeId, cancellationToken))
                errors.Add("Tip događaja sa zadatim Id ne postoji.");

            return errors;
        }
    }
}
