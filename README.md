# RaceDay

RaceDay is a full-stack, cloud-aware event management system built for the South African road running, walking and cycling community. It replaces paper-based registration, spreadsheets and disconnected communication with one platform.

**Event Organisers** create events, define categories, view enrolments and publish results. **Participants** browse upcoming events, enrol in a category, and track their enrolments and race history.

| Part | What | Where |
|---|---|---|
| 1 | ERD, API endpoint plan, SQL script | `docs/` |
| 2 | RESTful API (ASP.NET Core, EF Core Code-First, Swagger, unit tests) | `src/RaceDay.Api`, `tests/RaceDay.Tests` |
| 3 | MVC web app, Azure Blob Storage, Docker | `src/RaceDay.Web`, `docker-compose.yml` |

## Roles

| Role | Capabilities |
|---|---|
| **Organiser** | Dashboard; create, edit and delete events (with banner image); manage categories; view all enrolments for their events; capture and publish results. |
| **Participant** | Browse and filter events; view event details; enrol by choosing a category; view/cancel their enrolments; view personal race history; edit profile and upload a profile picture. |

Roles are enforced on the API (`[Authorize(Roles = ...)]` plus ownership checks) and reflected in the MVC app (separate navigation and pages for each role; protected pages redirect unauthenticated users to the login page).

## Architecture

```
Browser  -->  RaceDay.Web (MVC)  -->  RaceDay.Api (REST)  -->  SQL Server
                                                         -->  Azure Blob Storage
```

- The **MVC app has no database access and no business rules**. It only calls the API through one service (`Services/ApiClient.cs`).
- **Azure Blob Storage is used only by the API.** The MVC app sends image files to the API, and the API stores them in Azure. The browser then displays an image straight from its public blob URL.
- **Sessions:** logging in through MVC calls `POST /api/auth/login`. The API starts a session and returns a session cookie. MVC keeps that cookie inside its own encrypted login cookie (together with the user's id, name and role) and forwards it on every API call. If the API session ends (HTTP 401), the user is signed out and sent to the login page.

## Running locally

You need the .NET 8 SDK, SQL Server and (for images) an Azure Storage account.

### 1. API
1. Set `ConnectionStrings:DefaultConnection` in `src/RaceDay.Api/appsettings.json` for your SQL Server.
2. Create or update the database:
   ```
   cd src/RaceDay.Api
   dotnet ef database update
   ```
3. Store your Azure connection string in **user secrets** (never commit it):
   ```
   dotnet user-secrets init
   dotnet user-secrets set "AzureStorage:ConnectionString" "<your storage account connection string>"
   ```
4. `dotnet run` – the API listens on `http://localhost:5080` (Swagger at `/swagger`).

### 2. Web app
```
cd src/RaceDay.Web
dotnet run
```
Open `http://localhost:5090`. The API address is `ApiSettings:BaseUrl` in `src/RaceDay.Web/appsettings.json`.

### Azure Blob Storage setup
1. Create a **Storage account** in the Azure portal.
2. In the account's **Configuration** page, turn on **Allow Blob anonymous access** (needed so browsers can display the uploaded images).
3. Copy the connection string from **Security + networking > Access keys** into user secrets (above).
4. The API creates the container `raceday-images` automatically on the first upload.

## Docker

The MVC app runs in a container. The API keeps running on your machine and the container reaches it through `host.docker.internal`.

1. Start the API (`dotnet run` in `src/RaceDay.Api`). If the container cannot connect, start it on all interfaces instead: `dotnet run --urls "http://0.0.0.0:5080"`.
2. Build and run the web app container:

   **Docker run command**
   ```
   docker build -t raceday-web ./src/RaceDay.Web
   docker run -d --name raceday-web -p 8081:8080 -e ApiSettings__BaseUrl=http://host.docker.internal:5080 raceday-web
   ```
   **or with one command (Docker Compose)**
   ```
   docker compose up --build
   ```
3. Open `http://localhost:8081`. It behaves exactly like the locally running app.

Stop it with `docker rm -f raceday-web` (or `docker compose down`).

The Dockerfile uses a multi-stage build (SDK image to publish, small ASP.NET runtime image to run), restores packages in a separate cached layer, runs as a non-root user, and listens on port 8080.

## Part 3 changes to the API

The brief requires image uploads and a few extra data points, so the Part 2 API gained:

| Change | Why |
|---|---|
| `Azure.Storage.Blobs` service, `POST /api/events/{id}/banner`, `POST /api/users/me/profile-picture` | Banner and profile picture uploads go through the API into Azure Blob Storage. Files are checked (JPG/PNG/WebP, max 5 MB, real image header). |
| `BannerImageUrl` on `Events`, `ProfilePictureUrl` on `UserProfiles` (new migration) | Stores the blob URLs. |
| `GET /api/events/mine` | Organiser dashboard: own events with confirmed enrolment totals. |
| `totalFinishers` on results | Lets the My Results page show positions such as "47th of 312". |
| 10 new unit tests (56 in total) | Cover uploads, ownership, dashboard totals and finisher totals. |

## Tests and CI/CD

```
dotnet test
```

`.github/workflows/dotnet-ci.yml` runs on every push and pull request. It builds the whole solution (API, tests and MVC project), builds the MVC project explicitly, runs all unit tests, and then builds the MVC Docker image.

**Screenshot of a successful green build:**

<img width="1313" height="603" alt="image" src="https://github.com/user-attachments/assets/1b6cf767-4207-4aa3-9fd9-cc5145e7ed41" />


## Repository structure

```
.github/workflows/    dotnet-ci.yml, validate-structure.yml
docs/                 ERD, endpoint plan, SQL script (Part 1)
src/RaceDay.Api/      REST API (Part 2 + image uploads)
src/RaceDay.Web/      MVC web app, Dockerfile (Part 3)
tests/RaceDay.Tests/  xUnit tests for the API
docker-compose.yml
```

## Differences from the earlier plans

| Change | Reason |
|---|---|
| Part 2: `EventType` and `DistanceKM` added to `Events` | Required by the Part 2 brief. |
| Part 2: session cookie instead of JWT; `POST /api/auth/logout` added | The brief asks for session management. |
| Part 2: Organiser registration needs an organiser code | Stops users from making themselves Organisers. |
| Part 2: profile endpoint open to both roles | Brief says both roles can update their profile. |
| Part 3: enrolment statuses are Confirmed (green) and Cancelled (red) | Enrolment is confirmed immediately. An amber "Pending" style is defined in the CSS for future use. |
| Part 3: results are "published" as soon as an Organiser saves them | There is no separate draft state. |

## Video walkthrough

[https://youtu.be/U_hOaAez4TE](https://www.youtube.com/watch?v=U_hOaAez4TE)
