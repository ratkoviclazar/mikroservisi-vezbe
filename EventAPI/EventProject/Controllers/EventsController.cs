using EventAPI.CQRS.Abstractions;
using EventAPI.CQRS.Commands;
using EventAPI.CQRS.Queries;
using EventAPI.CQRS.Queries.ReadModels;
using EventAPI.DTO.Messaging.Saga.Choreography;
using EventAPI.DTO.Shared;
using EventAPI.DTOs;
using EventAPI.EventSourcing.Services;
using EventAPI.Messaging;
using EventProject.DTO.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace EventAPI.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private static int _counter = 0;
    private static int _timeoutCounter = 0;

    private readonly IQueryHandler<GetAllEventsQuery, List<EventListItemReadModel>> _getAllHandler;
    private readonly IQueryHandler<GetEventByIdQuery, EventDetailsReadModel?> _getByIdHandler;
    private readonly IQueryHandler<FilterEventsQuery, List<EventListItemReadModel>> _filterHandler;

    private readonly ICommandHandler<CreateEventCommand, CommandResult<int>> _createHandler;
    private readonly ICommandHandler<UpdateEventCommand, CommandResult> _updateHandler;
    private readonly ICommandHandler<DeleteEventCommand, CommandResult> _deleteHandler;

    private readonly IEventSourcingService _eventSourcingService;
    private readonly IChoreographyRabbitMqPublisher _choreographyPublisher;
    public EventsController(
        IQueryHandler<GetAllEventsQuery, List<EventListItemReadModel>> getAllHandler,
        IQueryHandler<GetEventByIdQuery, EventDetailsReadModel?> getByIdHandler,
        IQueryHandler<FilterEventsQuery, List<EventListItemReadModel>> filterHandler,
        ICommandHandler<CreateEventCommand, CommandResult<int>> createHandler,
        ICommandHandler<UpdateEventCommand, CommandResult> updateHandler,
        ICommandHandler<DeleteEventCommand, CommandResult> deleteHandler,
        IEventSourcingService eventSourcingService,
        IChoreographyRabbitMqPublisher choreographyPublisher)
    {
        _getAllHandler = getAllHandler;
        _getByIdHandler = getByIdHandler;
        _filterHandler = filterHandler;
        _createHandler = createHandler;
        _eventSourcingService = eventSourcingService;
        _updateHandler = updateHandler;
        _deleteHandler = deleteHandler;
        _choreographyPublisher = choreographyPublisher;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventListItemReadModel>>> GetAll()
    {
        //_counter++;

        //if (_counter % 10 != 0)
        //{
        //    Console.WriteLine("Simulating server error for testing purposes.");
        //    return StatusCode(500, "Simulated server error");
        //}

        var result = await _getAllHandler.HandleAsync(new GetAllEventsQuery());
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EventDetailsReadModel>> GetById(int id)
    {
        //_timeoutCounter++;
        //if (_timeoutCounter % 2 == 0)
        //{
        //    Console.WriteLine("Simulating timeout for testing purposes.");
        //    await Task.Delay(7000);
        //}

        var result = await _getByIdHandler.HandleAsync(new GetEventByIdQuery { Id = id });

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpGet("{id:int}/history")]
    public async Task<IActionResult> GetHistory(int id, CancellationToken cancellationToken)
    {
        var history = await _eventSourcingService.GetHistoryViewAsync(id, cancellationToken);

        if (history.Count == 0)
            return NotFound($"Istorija za događaj sa Id {id} ne postoji.");

        return Ok(history);
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<EventListItemReadModel>>> Filter(
        [FromQuery] string? name,
        [FromQuery] int? locationId,
        [FromQuery] int? typeId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var query = new FilterEventsQuery
        {
            NameContains = name,
            LocationId = locationId,
            TypeId = typeId,
            FromDate = from,
            ToDate = to
        };

        var result = await _filterHandler.HandleAsync(query);
        return Ok(result);
    }

    [HttpPost("{id:int}/change-location-choreography")]
    public async Task<IActionResult> ChangeLocationChoreography(
    int id,
    ChangeEventLocationChoreographyRequest request,
    CancellationToken ct)
    {
        var existingEvent = await _getByIdHandler.HandleAsync(
            new GetEventByIdQuery { Id = id },
            ct);

        if (existingEvent is null)
            return NotFound(new { error = $"Događaj sa Id={id} ne postoji." });

        if (existingEvent.LocationId == request.NewLocationId)
            return BadRequest(new { error = "Nova lokacija je ista kao trenutna lokacija." });

        var sagaId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();

        var message = new LocationChangeRequested
        {
            SagaId = sagaId,
            CorrelationId = correlationId,

            EventId = existingEvent.Id,
            OldLocationId = existingEvent.LocationId,
            NewLocationId = request.NewLocationId,

            EventName = existingEvent.Name,
            EventDateTime = existingEvent.DateTime
        };

        await _choreographyPublisher.PublishAsync(
            message: message,
            exchange: "reference.exchange",
            routingKey: RoutingKeys.LocationChangeRequested,
            messageType: nameof(LocationChangeRequested),
            ct: ct);

        return Accepted(new
        {
            SagaId = sagaId,
            CorrelationId = correlationId,
            Message = "Saga koreografija za promenu lokacije je pokrenuta."
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateEventDto dto)
    {
        var command = new CreateEventCommand
        {
            Name = dto.Name,
            Agenda = dto.Agenda,
            DateTime = dto.DateTime,
            DurationInHours = dto.DurationInHours,
            Price = dto.Price,
            TypeId = dto.TypeId,
            LocationId = dto.LocationId
        };

        var result = await _createHandler.HandleAsync(command);

        if (!result.Success)
            return MapFailure(result);

        return CreatedAtAction(nameof(GetById), new { id = result.Data }, new { id = result.Data });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateEventDto dto)
    {
        var command = new UpdateEventCommand
        {
            Id = id,
            Name = dto.Name,
            Agenda = dto.Agenda,
            DateTime = dto.DateTime,
            DurationInHours = dto.DurationInHours,
            Price = dto.Price,
            TypeId = dto.TypeId,
            LocationId = dto.LocationId
        };

        var result = await _updateHandler.HandleAsync(command);

        if (!result.Success)
            return MapFailure(result);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _deleteHandler.HandleAsync(new DeleteEventCommand { Id = id });

        if (!result.Success)
            return MapFailure(result);

        return NoContent();
    }

    private IActionResult MapFailure(CommandResult result) => result.Status switch
    {
        CommandStatus.NotFound => NotFound(new { errors = result.Errors }),
        CommandStatus.ValidationError => BadRequest(new { errors = result.Errors }),
        _ => StatusCode(500, new { errors = result.Errors })
    };
}
