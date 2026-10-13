# RaceDay

RaceDay is a full-stack, cloud-aware event management system built for the South African road running, walking and cycling community. It replaces the paper-based registration, spreadsheets and disconnected communication that most community races currently rely on.

**Event Organisers** create and manage events, categories and participant results. **Participants** browse upcoming events, enter an event by choosing a category, and track their personal results.

This repository is built progressively across three parts:

- **Part 1 – System Planning and Database** (`/docs`): ERD, API endpoint plan, SQL Server script.
- **Part 2 – RESTful API** (`/src/RaceDay.Api`, `/tests/RaceDay.Tests`): ASP.NET Core Web API, EF Core Code-First, session authentication, Swagger, unit tests, GitHub Actions CI.
- **Part 3 – MVC Web Application**: consumes the API, Azure Blob Storage, Docker *(not started)*.

## Roles

| Role | Capabilities |
|---|---|
| **Organiser** | Create, edit and delete their events; manage event categories; capture and correct participant results; view all enrolments for their events. |
| **Participant** | Create an account; browse events; enter an event by selecting a category; view and cancel their own enrolments; view their own results. |

Both roles can view and update their own account/profile. Role-based access is enforced on the server with `[Authorize(Roles = ...)]`, and ownership is checked as well (an Organiser can only change *their own* events).

---

## Part 1 Contents (`/docs`)

| File | Description |
|---|---|
| `docs/ERD.png` | Entity Relationship Diagram – 6 entities with primary keys, foreign keys and cardinality. |
| `docs/ERD-UML.png` | The same model as a UML class diagram. |
| `docs/API-Endpoint-Plan.md` | Endpoint plan: Authentication, User Profile, Events, Categories, Event Enrolments, Results. |
| `docs/RaceDay-Database.sql` | SQL Server script that creates the schema and seeds sample data. |

---

## Part 2 – RESTful API

### Tech
ASP.NET Core 8 Web API (C#) · Entity Framework Core 8 (Code-First) · SQL Server · Swagger (Swashbuckle) · xUnit · GitHub Actions

### Project structure
```
RaceDay.sln
src/RaceDay.Api/
  Controllers/   Auth, Users, Events, Categories, Enrolments, Results
  Models/        Entities (AppUser, UserProfile, Event, Category, Enrolment, Result)
  Data/          RaceDayDbContext + query helpers
  DTOs/          Request/response objects with validation
  Program.cs     DI, cookie authentication, Swagger
tests/RaceDay.Tests/
  AuthTests, EventTests, RoleAccessTests, EnrolmentTests, ResultTests
```

### Running the API
1. Open `src/RaceDay.Api/appsettings.json` and set `ConnectionStrings:DefaultConnection` for your SQL Server instance (e.g. `Server=.\SQLEXPRESS;...`). The default database name is `RaceDayCodeFirstDB`, so it does not clash with the database created by the Part 1 script.
2. Create the database from the EF Core model:
```
   cd src/RaceDay.Api
   dotnet tool install --global dotnet-ef      (first time only)
   dotnet ef migrations add InitialCreate
   dotnet ef database update
```
   (Visual Studio Package Manager Console: `Add-Migration InitialCreate` then `Update-Database`.)
3. Run it: `dotnet run` (or press F5). Swagger opens at `http://localhost:5080/swagger`.

### Authentication and sessions
- `POST /api/auth/register` creates an account; `POST /api/auth/login` starts a **session**: the server issues an HttpOnly auth cookie (`RaceDay.Session`) holding the user's id and role. The cookie is sent automatically on every following request, and `POST /api/auth/logout` ends the session.
- Passwords are hashed with ASP.NET Core Identity's `PasswordHasher` (PBKDF2 + salt). The plain-text password is never stored.
- Non-public endpoints return **401** when not logged in and **403** when the role (or ownership) is wrong.
- **How Organiser registration is managed:** anyone can pick "Participant", but registering as an "Organiser" requires a valid `organiserCode` (configured in `Registration:OrganiserCode`, default `RACEDAY-ORG-2026`). Without it the API returns 403, so users cannot simply promote themselves.

### Using Swagger
1. Open `/swagger`, expand `POST /api/auth/register`, and register (Organiser: include `organiserCode`).
2. Call `POST /api/auth/login` – the browser stores the session cookie.
3. Try any protected endpoint. Each endpoint shows its purpose, expected body and response codes.

### Endpoints
See `docs/API-Endpoint-Plan.md` for the full table. Summary:

| Area | Endpoints |
|---|---|
| Auth | `POST /api/auth/register`, `POST /api/auth/login`, `POST /api/auth/logout` |
| User profile | `GET/PUT /api/users/me`, `PUT /api/users/me/profile`, `GET /api/users/{id}` (Organiser) |
| Events | `GET /api/events`, `GET /api/events/{id}`, `POST/PUT/DELETE /api/events[/{id}]` (Organiser), `GET /api/events/{id}/enrolments` (Organiser) |
| Categories | `GET/POST /api/events/{eventId}/categories`, `PUT/DELETE /api/categories/{id}` |
| Enrolments | `POST /api/categories/{categoryId}/enrolments`, `GET /api/enrolments/me`, `DELETE /api/enrolments/{id}` (Participant) |
| Results | `POST /api/enrolments/{id}/results`, `PUT /api/results/{id}` (Organiser), `GET /api/results/me` (Participant), `GET /api/events/{id}/results` |

### Unit tests
```
dotnet test
```
Tests start the real API in memory with an in-memory database (no SQL Server needed) and cover: registration and login, hashed passwords, event management by Organisers, role rejection for the wrong role and unauthenticated requests, enrolment (including duplicate, full category and cancellation) and results.

### CI/CD
- `.github/workflows/dotnet-ci.yml` restores, builds and runs all unit tests on every push and pull request to `main`.
- `.github/workflows/validate-structure.yml` checks the Part 1 `/docs` files are present.

**Screenshot of a successful green build:**

`[ INSERT SCREENSHOT OF GREEN GITHUB ACTIONS RUN HERE ]`

### Deviations from the Part 1 plan (and why)
| Change | Reason |
|---|---|
| `Events` table gains `EventType` (Run/Walk/Cycle) and `DistanceKM` columns. | The Part 2 brief requires each event to capture a distance and event type. |
| Authentication uses a **session cookie** instead of the JWT mentioned in the plan; `POST /api/auth/logout` added. | The Part 2 brief asks for session management to keep the user's authenticated state and role. |
| `POST /api/auth/register` accepts an optional `organiserCode` and returns 403 for a missing/invalid code when registering an Organiser. | Stops users from self-registering as Organisers. |
| `PUT /api/users/me/profile` is open to **both** roles (plan said Participant only). | The Part 2 brief says both roles must be able to update their own profile. |
| `DELETE /api/events/{id}` returns 409 if the event has enrolments. | Protects enrolment and result history. |
| `PUT /api/results/{id}` is allowed for the Organiser who owns the event. | Plan said "capturing Organiser"; event ownership is the consistent rule used everywhere else. |
| `GET /api/events` lists upcoming events by default (`includePast=true` for all). | Matches the plan's "upcoming events" wording. |
| Entity class is named `AppUser` (mapped to the `Users` table). | `User` clashes with `ControllerBase.User` in controllers. |
| EF Core creates the schema with no `CHECK` constraints or `GETDATE()` defaults; these rules are enforced in the API instead. | Keeps the model simple; behaviour is covered by validation and tests. |

## Video Walkthrough

`[ INSERT YOUTUBE LINK HERE ]`