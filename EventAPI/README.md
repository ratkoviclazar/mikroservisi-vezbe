# Event Management Microservices Architecture

## ?? Pregled arhitekture

Ovo je kompletan sistem za upravljanje doga?ajima baziran na **mikroservisnoj arhitekturi** sa slede?im komponentama:

### ??? Servisi

#### 1. **Event Service** (`EventProject`)
- **API**: `api/events`, `api/event-lectures`
- **Odgovornost**: Upravljanje doga?ajima i dodelom predava?a
- **Baza**: EventDb
- **Port**: 5000 (po defaultu)

#### 2. **Catalog Service** (`Catalog Service`)
- **API**: `api/locations`, `api/event-types`
- **Odgovornost**: Upravljanje lokacijama i tipovima doga?aja
- **Baza**: CatalogDb
- **Port**: 5002

#### 3. **Lecturer Service** (`Lecturer Service`)
- **API**: `api/lecturers`
- **Odgovornost**: Upravljanje predava?ima
- **Baza**: LecturerDb
- **Port**: 5003

#### 4. **Web Service** (`EventProject.WebService`)
- **Tip**: MVC sa Views
- **Odgovornost**: Presentacijski sloj - prikazuje podatke iz API-ja
- **Port**: 5001
- **Tehnologija**: Razor Views + HTTP klijenti

### ?? Shared Komponente

#### `EventProject.Shared`
- DTOs (Data Transfer Objects)
- Messaging interfejsi (`IEventBusPublisher`, `IEventBusSubscriber`)
- Integration Events (EventCreated, EventUpdated, EventDeleted, itd.)

#### `EventProject.Infrastructure`
- In-Memory Event Bus implementacija (za development)
- RabbitMQ Event Bus template (za production)
- DI Extension metode

## ?? Tok podataka

```
???????????????????????????
?  Web Service (MVC)      ?
?  (Views + HTTP klijenti)?
???????????????????????????
         ?
    ???????????????????????????????????????????????
    ?                           ?                   ?
    ?                           ?                   ?
????????????????         ????????????????   ????????????????
? Event API    ?         ? Catalog API  ?   ?Lecturer API  ?
?   Service    ?         ?   Service    ?   ?   Service    ?
????????????????         ????????????????   ????????????????
```

## ?? HTTP Komunikacija izme?u servisa

Web Service koristi **HTTP klijente** za komunikaciju sa API-jima:

```csharp
// EventApiClient - komunicira sa Event servisom
IEventApiClient eventClient = serviceProvider.GetService<IEventApiClient>();
var events = await eventClient.GetAllEventsAsync();

// CatalogApiClient - komunicira sa Catalog servisom
ICatalogApiClient catalogClient = serviceProvider.GetService<ICatalogApiClient>();
var locations = await catalogClient.GetAllLocationsAsync();

// LecturerApiClient - komunicira sa Lecturer servisom
ILecturerApiClient lecturerClient = serviceProvider.GetService<ILecturerApiClient>();
var lecturers = await lecturerClient.GetAllLecturersAsync();
```

## ?? Message Bus (Event-Driven Communication)

### In-Memory Event Bus (Development)
```csharp
// Kreiraj publisher
var publisher = serviceProvider.GetService<IEventBusPublisher>();
await publisher.PublishAsync("event.created", eventData);
```

### RabbitMQ Event Bus (Production)
- Template je pripremljen sa TODO ozna?avanjem
- Može se lako implementirati zamenom In-Memory servisa sa RabbitMQ implementacijom
- Sve integra?ní events su definisane u `EventProject.Shared.Events`

## ?? DTOs (Data Transfer Objects)

Svi DTOs su u `EventProject.DTO`:

```
EventDtos.cs
??? EventDto
??? EventDetailDto
??? CreateEventDto
??? UpdateEventDto
??? EventLectureDto
??? EventLectureDetailDto

CatalogDtos.cs
??? LocationDto
??? CreateLocationDto
??? UpdateLocationDto
??? EventTypeDto
??? CreateEventTypeDto
??? UpdateEventTypeDto

LecturerDtos.cs
??? LecturerDto
??? CreateLecturerDto
??? UpdateLecturerDto
```

## ?? Pokretanje aplikacije

### Development sa sve tri servise:

```powershell
# Terminal 1 - Event Service
cd EventProject
dotnet run

# Terminal 2 - Catalog Service
cd "Catalog Service"
dotnet run

# Terminal 3 - Lecturer Service
cd "Lecturer Service"
dotnet run

# Terminal 4 - Web Service
cd EventProject.WebService
dotnet run
```

### URLs:
- Web Service: `https://localhost:5001`
- Event API: `https://localhost:5000/api/events`
- Catalog API: `https://localhost:5002/api/locations`
- Lecturer API: `https://localhost:5003/api/lecturers`

## ?? Baze podataka

### Event Service
```sql
-- Events tabela
-- EventLectures tabela
```

### Catalog Service
```sql
-- Locations tabela
-- EventTypes tabela
```

### Lecturer Service
```sql
-- Lecturers tabela
```

Connection strings su u `appsettings.json` svakog servisa.

## ?? Proxy Kontroleri (Web Service)

Web Service ima proxy kontrolere koji komuniciraju sa API-jima:

### EventsProxyController
- `GET /events` - Prikazi sve doga?aje
- `GET /events/{id}` - Detalji doga?aja
- `GET /events/create` - Forma za kreiranje
- `POST /events/create` - Kreiraj doga?aj
- `GET /events/edit/{id}` - Forma za ažuriranje
- `POST /events/edit/{id}` - Ažuriraj doga?aj
- `GET /events/delete/{id}` - Forma za brisanje
- `POST /events/delete/{id}` - Obriši doga?aj

### LocationsProxyController
- `GET /locations` - Sve lokacije
- `GET /locations/create` - Forma za kreiranje
- `POST /locations/create` - Kreiraj lokaciju
- `GET /locations/edit/{id}` - Ažuriraj lokaciju
- `POST /locations/delete/{id}` - Obriši lokaciju

### LecturersProxyController
- `GET /lecturers` - Svi predava?i
- `GET /lecturers/{id}` - Detalji predava?a
- `GET /lecturers/create` - Forma za kreiranje
- `POST /lecturers/create` - Kreiraj predava?a
- `GET /lecturers/edit/{id}` - Ažuriraj predava?a
- `POST /lecturers/delete/{id}` - Obriši predava?a

## ?? Integration Events

### Event Service Events
- `EventCreatedIntegrationEvent`
- `EventUpdatedIntegrationEvent`
- `EventDeletedIntegrationEvent`
- `EventLectureCreatedIntegrationEvent`

### Catalog Service Events
- `LocationCreatedIntegrationEvent`
- `EventTypeCreatedIntegrationEvent`

### Lecturer Service Events
- `LecturerCreatedIntegrationEvent`
- `LecturerUpdatedIntegrationEvent`
- `LecturerDeletedIntegrationEvent`

## ?? RabbitMQ Integracija (TODO)

U fajlu `EventProject.Infrastructure/Messaging/RabbitMQ/RabbitMQEventBus.cs` nalaze se TODO dijelovi za implementaciju:

1. **Inicijalizacija konekcije**
2. **Publish metoda** - slanje poruka u RabbitMQ
3. **Subscribe metoda** - slušanje poruka iz RabbitMQ
4. **Hendlovanje poruka** - obrada primljenih poruka

## ?? Obavezne NuGet pakete

Kada budete implementirali RabbitMQ, trebat ?e vam:
```xml
<PackageReference Include="RabbitMQ.Client" Version="6.4.0" />
```

## ??? Struktura direktorijuma

```
EventAPI/
??? EventProject/                 # Event Service
?   ??? Models/
?   ??? Services/
?   ??? Controllers/
?   ??? Data/
?   ??? appsettings.json
??? Catalog Service/              # Catalog Service
?   ??? Models/
?   ??? Services/
?   ??? Controllers/
?   ??? Data/
?   ??? appsettings.json
??? Lecturer Service/             # Lecturer Service
?   ??? Models/
?   ??? Services/
?   ??? Controllers/
?   ??? Data/
?   ??? appsettings.json
??? EventProject.WebService/      # Web Service (MVC)
?   ??? Controllers/              # Proxy kontroleri
?   ??? Views/
?   ??? Services/                 # API klijenti
?   ??? appsettings.json
??? EventProject.Shared/          # Shared DTOs
??? EventProject.Infrastructure/  # Messaging
??? EventProject.DTO/             # DTOs
??? solution.sln
```

## ?? Napomene

- Svi servisi imaju **Swagger** dokumentaciju
- **Logging** je konfigurisan u svakom servisu
- **ModelValidation** je automatski omogu?en
- **Error handling** je centralizovan u proxy kontrolerima

## ?? Slede?i koraci - RabbitMQ Integracija

1. Instaliraj RabbitMQ na lokalnoj mašini ili Docker kontejneru
2. Implementiraj metode u `RabbitMQEventBus.cs`
3. Dodaj `AddRabbitMQEventBus()` u `Program.cs` svog servisa
4. Registruj event handlere u servisima
5. Testiraj message publishing izme?u servisa

---

**Autori**: Team Event Management  
**Verzija**: 1.0  
**.NET**: 8.0
