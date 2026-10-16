using Npgsql;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Tasks")
    ?? throw new InvalidOperationException("ConnectionStrings__Tasks must be set.");
builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));
builder.Services.AddProblemDetails();
var app = builder.Build();
app.UseExceptionHandler();

// A tiny, idempotent schema for this learning project. Use migrations as it grows.
await using (var connection = await app.Services.GetRequiredService<NpgsqlDataSource>().OpenConnectionAsync())
await using (var schema = new NpgsqlCommand("""
    CREATE TABLE IF NOT EXISTS tasks (
        id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
        title varchar(120) NOT NULL CHECK (length(trim(title)) > 0),
        completed boolean NOT NULL DEFAULT false,
        created_at timestamptz NOT NULL DEFAULT now()
    );
    """, connection))
    await schema.ExecuteNonQueryAsync();

app.MapGet("/health", async (NpgsqlDataSource db, CancellationToken ct) =>
{
    try
    {
        await using var command = db.CreateCommand("SELECT 1");
        await command.ExecuteScalarAsync(ct);
        return Results.Ok(new { status = "healthy", service = "tasks-api" });
    }
    catch (NpgsqlException) { return Results.StatusCode(503); }
});

app.MapGet("/tasks", async (NpgsqlDataSource db, CancellationToken ct) =>
{
    var tasks = new List<TaskItem>();
    await using var command = db.CreateCommand("SELECT id, title, completed, created_at FROM tasks ORDER BY id DESC");
    await using var reader = await command.ExecuteReaderAsync(ct);
    while (await reader.ReadAsync(ct)) tasks.Add(ReadTask(reader));
    return Results.Ok(tasks);
});

app.MapPost("/tasks", async (CreateTask request, NpgsqlDataSource db, CancellationToken ct) =>
{
    var title = request.Title?.Trim();
    if (string.IsNullOrEmpty(title) || title.Length > 120)
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["title"] = ["Enter a task between 1 and 120 characters."] });
    await using var command = db.CreateCommand("INSERT INTO tasks (title) VALUES ($1) RETURNING id, title, completed, created_at");
    command.Parameters.AddWithValue(title);
    await using var reader = await command.ExecuteReaderAsync(ct);
    await reader.ReadAsync(ct);
    var task = ReadTask(reader);
    return Results.Created($"/tasks/{task.Id}", task);
});

app.MapPut("/tasks/{id:int}", async (int id, UpdateTask request, NpgsqlDataSource db, CancellationToken ct) =>
{
    await using var command = db.CreateCommand("UPDATE tasks SET completed = $1 WHERE id = $2 RETURNING id, title, completed, created_at");
    command.Parameters.AddWithValue(request.Completed);
    command.Parameters.AddWithValue(id);
    await using var reader = await command.ExecuteReaderAsync(ct);
    return await reader.ReadAsync(ct) ? Results.Ok(ReadTask(reader)) : Results.NotFound();
});

app.MapDelete("/tasks/{id:int}", async (int id, NpgsqlDataSource db, CancellationToken ct) =>
{
    await using var command = db.CreateCommand("DELETE FROM tasks WHERE id = $1");
    command.Parameters.AddWithValue(id);
    return await command.ExecuteNonQueryAsync(ct) == 1 ? Results.NoContent() : Results.NotFound();
});

await app.RunAsync();
return 0;

static TaskItem ReadTask(NpgsqlDataReader reader) => new(reader.GetInt32(0), reader.GetString(1), reader.GetBoolean(2), reader.GetDateTime(3));
record TaskItem(int Id, string Title, bool Completed, DateTime CreatedAt);
record CreateTask(string? Title);
record UpdateTask(bool Completed);
