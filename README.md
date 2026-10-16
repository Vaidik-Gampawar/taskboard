# TaskBoard

A small three-tier .NET task tracker:

```text
Browser → TaskBoard.Web → TaskBoard.Api → PostgreSQL
             port 8080       port 8081
```

The web frontend and API are separate .NET 10 services. PostgreSQL stores tasks. You can add, complete, reopen, and delete tasks.

## Requirements

- .NET 10 SDK
- PostgreSQL 17 or later
- Python 3 only if running the optional smoke test

## Set up the database

Connect to PostgreSQL as an administrator using psql or pgAdmin and run:

```sql
CREATE ROLE taskboard WITH LOGIN PASSWORD 'replace-with-your-password';
CREATE DATABASE taskboard OWNER taskboard;
```

The API creates its table automatically on startup. Use a password without semicolons for the connection-string examples below, or escape it appropriately.

## Run the project

From this folder:

```sh
dotnet build TaskBoard.sln
```

Terminal 1 — start the API with your database password:

```sh
ConnectionStrings__Tasks='Host=localhost;Port=5432;Database=taskboard;Username=taskboard;Password=replace-with-your-password' dotnet run --project src/TaskBoard.Api
```

For PowerShell:

```powershell
$env:ConnectionStrings__Tasks='Host=localhost;Port=5432;Database=taskboard;Username=taskboard;Password=replace-with-your-password'
dotnet run --project src/TaskBoard.Api
```

Terminal 2 — start the web frontend:

```sh
dotnet run --project src/TaskBoard.Web
```

Open **http://localhost:8080**. The included launch profiles select ports 8080 for Web and 8081 for API. By default, Web connects to `http://localhost:8081/`.

## Configuration

| Service | Environment variable | Purpose |
| --- | --- | --- |
| API | `ConnectionStrings__Tasks` | Required PostgreSQL connection string |
| Web | `ApiBaseUrl` | API base URL, including trailing `/`; defaults to `http://localhost:8081/` |
| Both | `ASPNETCORE_URLS` | Listening URL when running without a launch profile |

For example, run without a launch profile with `ASPNETCORE_URLS=http://localhost:8080 dotnet run --no-launch-profile --project src/TaskBoard.Web`. Configure the API listening URL and database connection similarly. Keep database credentials outside source control.

## API endpoints

The browser calls the public paths on Web, which forwards them to API. API routes omit the `/api` prefix.

| Method | Public path | Body |
| --- | --- | --- |
| GET | `/api/tasks` | — |
| POST | `/api/tasks` | `{"title":"Learn something new"}` |
| PUT | `/api/tasks/1` | `{"completed":true}` |
| DELETE | `/api/tasks/1` | — |
| GET | `/health` | — |

Titles must be 1–120 characters after trimming. Queries use SQL parameters. The web health endpoint checks API and database connectivity.

## Optional verification

With both services and PostgreSQL running:

```sh
python3 scripts/smoke-test.py
```

This tests the homepage, readiness, validation, and task operations through all three tiers, and removes its test task afterward.

## Project layout

- `src/TaskBoard.Web`: web host, API forwarding, and HTML/CSS/JavaScript frontend.
- `src/TaskBoard.Api`: REST endpoints and PostgreSQL access using Npgsql.
- `scripts/smoke-test.py`: end-to-end smoke test using only the Python standard library.

This is a shared learning app with no sign-in. Anyone with access can modify tasks. The simple table initialization is intended for this small project; use migrations when evolving the schema.
