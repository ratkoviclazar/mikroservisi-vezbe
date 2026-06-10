# API Specifikacija - Event Management System

## Event Service API

### Base URL
```
https://localhost:5000/api
```

### Endpoints

#### Events

| Metod | Endpoint | Opis |
|-------|----------|------|
| `GET` | `/events` | Dobija sve doga?aje |
| `GET` | `/events/{id}` | Dobija doga?aj po ID-u |
| `POST` | `/events` | Kreira novi doga?aj |
| `PUT` | `/events/{id}` | Ažurira doga?aj |
| `DELETE` | `/events/{id}` | Briše doga?aj |

##### GET /events
```bash
curl -X GET "https://localhost:5000/api/events"
```

**Response (200 OK)**:
```json
[
  {
    "id": 1,
    "name": "Tech Conference",
    "agenda": "Latest tech trends",
    "dateTime": "2025-05-01T10:00:00",
    "durationInHours": 8,
    "price": 100.00,
    "typeId": 1,
    "locationId": 1
  }
]
```

##### POST /events
```bash
curl -X POST "https://localhost:5000/api/events" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "New Event",
    "agenda": "Event description",
    "dateTime": "2025-06-01T10:00:00",
    "durationInHours": 4,
    "price": 50.00,
    "typeId": 1,
    "locationId": 1
  }'
```

**Response (201 Created)**:
```json
{
  "id": 2,
  "name": "New Event",
  "agenda": "Event description",
  "dateTime": "2025-06-01T10:00:00",
  "durationInHours": 4,
  "price": 50.00,
  "typeId": 1,
  "locationId": 1
}
```

#### Event Lectures

| Metod | Endpoint | Opis |
|-------|----------|------|
| `GET` | `/event-lectures` | Dobija sva predavanja |
| `GET` | `/event-lectures/{id}` | Dobija predavanje po ID-u |
| `GET` | `/event-lectures/by-event/{eventId}` | Dobija sva predavanja za doga?aj |
| `POST` | `/event-lectures` | Kreira novo predavanje |
| `PUT` | `/event-lectures/{id}` | Ažurira predavanje |
| `DELETE` | `/event-lectures/{id}` | Briše predavanje |

---

## Catalog Service API

### Base URL
```
https://localhost:5002/api
```

### Endpoints

#### Locations

| Metod | Endpoint | Opis |
|-------|----------|------|
| `GET` | `/locations` | Dobija sve lokacije |
| `GET` | `/locations/{id}` | Dobija lokaciju po ID-u |
| `POST` | `/locations` | Kreira novu lokaciju |
| `PUT` | `/locations/{id}` | Ažurira lokaciju |
| `DELETE` | `/locations/{id}` | Briše lokaciju |

##### GET /locations
```bash
curl -X GET "https://localhost:5002/api/locations"
```

**Response (200 OK)**:
```json
[
  {
    "id": 1,
    "name": "Hall A",
    "address": "Main Street 123",
    "capacity": 500
  }
]
```

##### POST /locations
```bash
curl -X POST "https://localhost:5002/api/locations" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "New Hall",
    "address": "Street 456",
    "capacity": 300
  }'
```

#### Event Types

| Metod | Endpoint | Opis |
|-------|----------|------|
| `GET` | `/event-types` | Dobija sve tipove doga?aja |
| `GET` | `/event-types/{id}` | Dobija tip po ID-u |
| `POST` | `/event-types` | Kreira novi tip |
| `PUT` | `/event-types/{id}` | Ažurira tip |
| `DELETE` | `/event-types/{id}` | Briše tip |

---

## Lecturer Service API

### Base URL
```
https://localhost:5003/api
```

### Endpoints

#### Lecturers

| Metod | Endpoint | Opis |
|-------|----------|------|
| `GET` | `/lecturers` | Dobija sve predava?e |
| `GET` | `/lecturers/{id}` | Dobija predava?a po ID-u |
| `POST` | `/lecturers` | Kreira novog predava?a |
| `PUT` | `/lecturers/{id}` | Ažurira predava?a |
| `DELETE` | `/lecturers/{id}` | Briše predava?a |

##### GET /lecturers
```bash
curl -X GET "https://localhost:5003/api/lecturers"
```

**Response (200 OK)**:
```json
[
  {
    "id": 1,
    "name": "John",
    "surname": "Doe",
    "title": "Professor",
    "expertiseArea": "Software Architecture"
  }
]
```

##### POST /lecturers
```bash
curl -X POST "https://localhost:5003/api/lecturers" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Jane",
    "surname": "Smith",
    "title": "Dr",
    "expertiseArea": "Artificial Intelligence"
  }'
```

---

## Error Responses

### 400 Bad Request
```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "name": ["The name field is required."]
  }
}
```

### 404 Not Found
```json
{
  "title": "Not Found",
  "detail": "Event with id 999 not found",
  "status": 404
}
```

### 500 Internal Server Error
```json
{
  "title": "Internal Server Error",
  "detail": "Error retrieving events",
  "status": 500
}
```

---

## DTOs (Data Transfer Objects)

### Event DTOs
```csharp
public class EventDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Agenda { get; set; }
    public DateTime DateTime { get; set; }
    public decimal DurationInHours { get; set; }
    public decimal Price { get; set; }
    public int TypeId { get; set; }
    public int LocationId { get; set; }
}

public class CreateEventDto
{
    public string Name { get; set; }
    public string Agenda { get; set; }
    public DateTime DateTime { get; set; }
    public decimal DurationInHours { get; set; }
    public decimal Price { get; set; }
    public int TypeId { get; set; }
    public int LocationId { get; set; }
}
```

### Location DTOs
```csharp
public class LocationDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
    public int Capacity { get; set; }
}

public class CreateLocationDto
{
    public string Name { get; set; }
    public string Address { get; set; }
    public int Capacity { get; set; }
}
```

### Lecturer DTOs
```csharp
public class LecturerDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Surname { get; set; }
    public string Title { get; set; }
    public string ExpertiseArea { get; set; }
}

public class CreateLecturerDto
{
    public string Name { get; set; }
    public string Surname { get; set; }
    public string Title { get; set; }
    public string ExpertiseArea { get; set; }
}
```

---

## Autentifikacija i Autorizacija

Trenutno nema implementirane autentifikacije. Za production:

1. Dodaj JWT autentifikaciju
2. Implementiraj role-based authorization
3. Zaštiti sensitive endpoints

---

## Rate Limiting

Nije implementirano. Preporuka: Dodaj rate limiting za production:

```csharp
app.UseRateLimiter();
```

---

## Swagger Documentation

Svaki API servis ima Swagger dokumentaciju dostupnu na:

- Event Service: `https://localhost:5000/swagger`
- Catalog Service: `https://localhost:5002/swagger`
- Lecturer Service: `https://localhost:5003/swagger`

---

## Verzije API-ja

Trenutna verzija: **v1**

Budu?nost: Razmotri versioniranje API-ja ako dodaš nove endpointe.

