using EventAPI.CQRS.Abstractions;
using EventAPI.CQRS.Queries.ReadModels;

namespace EventAPI.CQRS.Queries
{
    public sealed class GetAllEventsQuery : IQuery<List<EventListItemReadModel>>
    {
    }
}
