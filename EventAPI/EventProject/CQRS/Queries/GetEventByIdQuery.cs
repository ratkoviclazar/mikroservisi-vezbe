using EventAPI.CQRS.Abstractions;
using EventAPI.CQRS.Queries.ReadModels;

namespace EventAPI.CQRS.Queries
{

    public sealed class GetEventByIdQuery : IQuery<EventDetailsReadModel?>
    {
        public int Id { get; init; }
    }
}
