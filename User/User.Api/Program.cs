using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using User.Db.MSSQL.EF;
using User.Db.MSSQL.EF.DB;
using User.Svc;

var builder = WebApplication.CreateBuilder(args);

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

var cs = configuration.GetConnectionString("Default")
         ?? throw new InvalidOperationException("Missing connection string 'Default'.");

builder.Services.AddDbContext<UsrDbContext>(opt =>
    opt.UseSqlServer(cs, sql => { sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null); sql.CommandTimeout(60); }));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<ITimeProvider, SystemTimeProvider>();
// builder.Services.AddScoped<IUsersSvc, EFUsrsSvc>(); //Entity Connection
builder.Services.AddSingleton<IDbConnectionFactory>(_ => new SqlConnectionFactory(cs)); // Dapper Connection
builder.Services.AddScoped<IUsersSvc, DpUsersSvc>(); // Dapper Connection

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Endpoints: inject service per-request
app.MapGet("/usrs", ([FromServices] IUsersSvc svc) => svc.GetAll())
   .WithName("get-usrs");

app.MapGet("/usr/{id}", ([FromRoute] string id, [FromServices] IUsersSvc svc) =>
{
    if (string.IsNullOrWhiteSpace(id))
        return Results.BadRequest("Request: Id is required.");

    var itm = svc.GetById(id);
    return itm is null ? Results.NotFound($"Usr: {id} not found.") : Results.Ok(itm);
}).WithName("dict-get-user-by-id");

app.MapPost("/usr", ([FromBody] User.Svc.Usr dto, [FromServices] IUsersSvc svc) =>
{
    var msg = svc.Create(dto);
    if (!string.IsNullOrEmpty(msg)) return Results.BadRequest(msg);
    return Results.CreatedAtRoute("dict-get-user-by-id", new { id = dto.Id }, new { dto.FullName, dto.Active });
}).WithName("create-usr");

app.MapPut("/usr", ([FromBody] User.Svc.Usr dto, [FromServices] IUsersSvc svc) =>
{
    var msg = svc.Update(dto);
    if (!string.IsNullOrEmpty(msg)) return Results.BadRequest(msg);
    return Results.Ok($"User '{dto.Id}' updated!");
}).WithName("update-usr");

app.MapDelete("/usr/{id}", ([FromRoute] string id, [FromServices] IUsersSvc svc) =>
{
    var msg = svc.DelById(id);
    if (!string.IsNullOrEmpty(msg)) return Results.BadRequest(msg);
    return Results.Ok($"Deleted user {id}");
}).WithName("delete-usr-by-id");

app.Run();

public interface ITimeProvider { DateTime Now { get; } }
public class SystemTimeProvider : ITimeProvider
{
    public DateTime Now => DateTime.Now;
}
