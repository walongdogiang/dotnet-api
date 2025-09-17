using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<ITimeProvider, SystemTimeProvider>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/time", (ITimeProvider timeProvider) =>
{
    return $"Current time: {timeProvider.Now}";
});

app.MapGet("/hdr-cli-id", ([FromHeader(Name = "X-CI-Id")] string clientId) =>
{
    return $"Header: Client Id: {clientId}";
})
.WithName("hdeader-get-client-id");

app.MapGet("/hdr-req-inf", ([FromHeader(Name = "X-CI-Id")] string clientId,
                            [FromHeader(Name = "X-Rq-Id")] string requestId) =>
{
    return $"Header: Client Id: {clientId}, Request Id: {requestId}";
})
.WithName("header-get-request-info");

app.MapGet("/a-usrs", () =>
{
    return new[] { "us1", "us2", "us3" };
})
.WithName("array-get-users");

app.MapPost("/upload",
async (IFormFile file, string? name) =>
{
    if (file is null || file.Length == 0) return Results.BadRequest("No file");

    var uploads = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "uploads");
    Directory.CreateDirectory(uploads);

    var path = Path.Combine(uploads, Path.GetRandomFileName());
    using var fs = File.Create(path);
    await file.CopyToAsync(fs);

    return Results.Ok(new { name, saved = Path.GetFileName(path) });
})
.WithName("upload")
.DisableAntiforgery();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
app.MapGet("/tdb", async () =>
{
    try
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        return Results.Ok("Connected to the database successfully!");
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to connect: {ex.Message}");
    }
})
.WithName("test-azure-db-connection");

app.Run();

public interface ITimeProvider { DateTime Now { get; } }
public class SystemTimeProvider : ITimeProvider
{
    public DateTime Now => DateTime.Now;
}