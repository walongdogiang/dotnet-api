using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using User.Svc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<ITimeProvider, SystemTimeProvider>();
builder.Services.AddSingleton<IUsersSvc, UsersSvc>();

var app = builder.Build();
var svc = app.Services.GetRequiredService<IUsersSvc>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/usrs", () =>
{
    return svc.GetAll();
}).WithName("get-usrs");

app.MapGet("/usr/{id}", ([FromRoute] string id) =>
{
    if (string.IsNullOrWhiteSpace(id))
        return Results.BadRequest($"Request: Id is required.");

    var itm = svc.GetById(id);

    if (itm == null)
        return Results.NotFound($"Usr: {id} not found.");

    return Results.Ok(itm);
})
.WithName("dict-get-user-by-id");

app.MapPost("/usr", ([FromBody] User.Svc.Usr dto) =>
{
    var msg = svc.Create(dto);
    if(!string.IsNullOrEmpty(msg))
        return Results.BadRequest(msg);
    return Results.CreatedAtRoute(
        routeName: "dict-get-user-by-id",
        routeValues: new { id = dto.Id },
        value: new { dto.FullName, dto.Active }
    );
}).WithName("create-usr");

app.MapPut("/usr", ([FromBody] User.Svc.Usr dto) =>
{

    var msg = svc.Update(dto);
    if (!string.IsNullOrEmpty(msg))
        return Results.BadRequest(msg);
    return Results.Ok($"User '{dto.Id}' updated!'.");
}).WithName("update-usr");

app.MapDelete("/usr/{id}", ([FromRoute] string id) =>
{
    var msg = svc.DelById(id);
    if (!string.IsNullOrEmpty(msg))
        return Results.BadRequest(msg);
    return Results.Ok($"Deleted user {id}"); 
}).WithName("delete-usr-by-id");

app.Run();

public interface ITimeProvider { DateTime Now { get; } }
public class SystemTimeProvider : ITimeProvider
{
    public DateTime Now => DateTime.Now;
}
