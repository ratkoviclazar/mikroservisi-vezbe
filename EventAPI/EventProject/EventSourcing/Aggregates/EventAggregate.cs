using EventAPI.Domain.Events;
using EventAPI.EventSourcing.Persistence;

namespace EventAPI.EventSourcing.Aggregates
{
    public sealed class EventAggregate
    {
        private readonly List<DomainEvent> _uncommittedEvents = new();

        public int Id { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string Agenda { get; private set; } = string.Empty;
        public DateTime DateTime { get; private set; }
        public decimal DurationInHours { get; private set; }
        public decimal Price { get; private set; }
        public int TypeId { get; private set; }
        public int LocationId { get; private set; }
        public bool IsDeleted { get; private set; }

        public int Version { get; private set; }

        public IReadOnlyCollection<DomainEvent> UncommittedEvents => _uncommittedEvents.AsReadOnly();

        public EventAggregate()
        {
        }

        public static EventAggregate Create(
            int id,
            string name,
            string agenda,
            DateTime dateTime,
            decimal durationInHours,
            decimal price,
            int typeId,
            int locationId)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("Naziv događaja je obavezan.");

            if (dateTime <= DateTime.Now)
                throw new InvalidOperationException("Datum događaja mora biti u budućnosti.");

            if (durationInHours <= 0)
                throw new InvalidOperationException("Trajanje događaja mora biti veće od 0.");

            if (price < 0)
                throw new InvalidOperationException("Cena ne može biti negativna.");

            if (typeId <= 0)
                throw new InvalidOperationException("Tip događaja nije validan.");

            if (locationId <= 0)
                throw new InvalidOperationException("Lokacija nije validna.");

            var aggregate = new EventAggregate();

            aggregate.Raise(new EventCreated
            {
                AggregateId = id,
                Name = name,
                Agenda = agenda,
                DateTime = dateTime,
                DurationInHours = durationInHours,
                Price = price,
                TypeId = typeId,
                LocationId = locationId
            });

            return aggregate;
        }

        public EventAggregateSnapshotState ToSnapshotState()
        {
            return new EventAggregateSnapshotState
            {
                Id = Id,
                Name = Name,
                Agenda = Agenda,
                DateTime = DateTime,
                DurationInHours = DurationInHours,
                Price = Price,
                TypeId = TypeId,
                LocationId = LocationId,
                IsDeleted = IsDeleted,
                Version = Version
            };
        }

        public static EventAggregate FromSnapshotState(EventAggregateSnapshotState state)
        {
            return new EventAggregate
            {
                Id = state.Id,
                Name = state.Name,
                Agenda = state.Agenda,
                DateTime = state.DateTime,
                DurationInHours = state.DurationInHours,
                Price = state.Price,
                TypeId = state.TypeId,
                LocationId = state.LocationId,
                IsDeleted = state.IsDeleted,
                Version = state.Version
            };
        }

        public static EventAggregate LoadFromHistory(IEnumerable<DomainEvent> events)
        {
            var aggregate = new EventAggregate();

            foreach (var domainEvent in events.OrderBy(x => x.Version))
            {
                aggregate.Apply(domainEvent);
                aggregate.Version = domainEvent.Version;
            }

            return aggregate;
        }

        public void ChangeName(string name)
        {
            EnsureNotDeleted();

            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("Naziv događaja je obavezan.");

            if (Name == name)
                return;

            Raise(new EventNameChanged
            {
                AggregateId = Id,
                Name = name
            });
        }

        public void ChangeAgenda(string agenda)
        {
            EnsureNotDeleted();

            if (Agenda == agenda)
                return;

            Raise(new EventAgendaChanged
            {
                AggregateId = Id,
                Agenda = agenda
            });
        }

        public void ChangeDateTime(DateTime dateTime)
        {
            EnsureNotDeleted();

            if (dateTime <= DateTime.Now)
                throw new InvalidOperationException("Datum događaja mora biti u budućnosti.");

            if (DateTime == dateTime)
                return;

            Raise(new EventDateTimeChanged
            {
                AggregateId = Id,
                DateTime = dateTime
            });
        }

        public void ChangeDuration(decimal durationInHours)
        {
            EnsureNotDeleted();

            if (durationInHours <= 0)
                throw new InvalidOperationException("Trajanje događaja mora biti veće od 0.");

            if (DurationInHours == durationInHours)
                return;

            Raise(new EventDurationChanged
            {
                AggregateId = Id,
                DurationInHours = durationInHours
            });
        }

        public void ChangePrice(decimal price)
        {
            EnsureNotDeleted();

            if (price < 0)
                throw new InvalidOperationException("Cena ne može biti negativna.");

            if (Price == price)
                return;

            Raise(new EventPriceChanged
            {
                AggregateId = Id,
                Price = price
            });
        }

        public void ChangeType(int typeId)
        {
            EnsureNotDeleted();

            if (typeId <= 0)
                throw new InvalidOperationException("Tip događaja nije validan.");

            if (TypeId == typeId)
                return;

            Raise(new EventTypeChanged
            {
                AggregateId = Id,
                TypeId = typeId
            });
        }

        public void ChangeLocation(int locationId)
        {
            EnsureNotDeleted();

            if (locationId <= 0)
                throw new InvalidOperationException("Lokacija nije validna.");

            if (LocationId == locationId)
                return;

            Raise(new EventLocationChanged
            {
                AggregateId = Id,
                LocationId = locationId
            });
        }

        public void Delete()
        {
            EnsureNotDeleted();

            Raise(new EventDeleted
            {
                AggregateId = Id
            });
        }

        public void ClearUncommittedEvents()
        {
            _uncommittedEvents.Clear();
        }

        private void Raise(DomainEvent domainEvent)
        {
            domainEvent.Version = Version + 1;
            domainEvent.OccurredAt = DateTime.UtcNow;

            Apply(domainEvent);

            _uncommittedEvents.Add(domainEvent);
        }

        private void Apply(DomainEvent domainEvent)
        {
            switch (domainEvent)
            {
                case EventCreated e:
                    Apply(e);
                    break;

                case EventNameChanged e:
                    Apply(e);
                    break;

                case EventAgendaChanged e:
                    Apply(e);
                    break;

                case EventDateTimeChanged e:
                    Apply(e);
                    break;

                case EventDurationChanged e:
                    Apply(e);
                    break;

                case EventPriceChanged e:
                    Apply(e);
                    break;

                case EventTypeChanged e:
                    Apply(e);
                    break;

                case EventLocationChanged e:
                    Apply(e);
                    break;

                case EventDeleted e:
                    Apply(e);
                    break;

                default:
                    throw new InvalidOperationException($"Nepoznat event tip: {domainEvent.GetType().Name}");
            }

            Version = domainEvent.Version;
        }

        private void Apply(EventCreated e)
        {
            Id = e.AggregateId;
            Name = e.Name;
            Agenda = e.Agenda;
            DateTime = e.DateTime;
            DurationInHours = e.DurationInHours;
            Price = e.Price;
            TypeId = e.TypeId;
            LocationId = e.LocationId;
            IsDeleted = false;
        }

        private void Apply(EventNameChanged e)
        {
            Name = e.Name;
        }

        private void Apply(EventAgendaChanged e)
        {
            Agenda = e.Agenda;
        }

        private void Apply(EventDateTimeChanged e)
        {
            DateTime = e.DateTime;
        }

        private void Apply(EventDurationChanged e)
        {
            DurationInHours = e.DurationInHours;
        }

        private void Apply(EventPriceChanged e)
        {
            Price = e.Price;
        }

        private void Apply(EventTypeChanged e)
        {
            TypeId = e.TypeId;
        }

        private void Apply(EventLocationChanged e)
        {
            LocationId = e.LocationId;
        }

        private void Apply(EventDeleted e)
        {
            IsDeleted = true;
        }

        public void ApplyFromHistory(DomainEvent domainEvent)
        {
            Apply(domainEvent);
        }

        private void EnsureNotDeleted()
        {
            if (IsDeleted)
                throw new InvalidOperationException("Događaj je obrisan i ne može se menjati.");
        }
    }
}
