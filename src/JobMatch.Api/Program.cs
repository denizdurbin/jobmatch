using JobMatch.Api.Endpoints;
using JobMatch.Infrastructure;
using JobMatch.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// apply migrations on startup, fine for a local project
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.MapOpenApi();
app.MapScalarApiReference();

app.MapGet("/health", async (AppDbContext db, CancellationToken ct) =>
{
    bool dbOk = await db.Database.CanConnectAsync(ct);
    if (!dbOk)
    {
        return Results.Problem("Database is not reachable", statusCode: 503);
    }
    return Results.Ok(new { status = "healthy", database = "ok" });
}).WithTags("Health");

app.MapPostingEndpoints();

app.Run();
