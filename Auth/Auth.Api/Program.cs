using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using Auth.Svc.AccsSvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<ITimeProvider, SystemTimeProvider>();
builder.Services.AddSingleton<IAccsSvc, AccsSvc>();

var app = builder.Build();
var svc = app.Services.GetRequiredService<IAccsSvc>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/d-accs", () =>
{
    return svc.GetAll();
})
.WithName("dict-get-accounts");

app.MapPost("/d-acc", (AccountCreate user) =>
{
    if (user == null || string.IsNullOrEmpty(user.Username) || string.IsNullOrEmpty(user.Password))
        return Results.BadRequest("Username and password cannot empty!");
    var rs = svc.Register(user.Username, user.Password);
    return Results.Ok(rs);
})
.WithName("dict-create-account");

app.MapPost("/d-acc-pwd/{username}", (string username, PasswordDto body) =>
{
    if (string.IsNullOrEmpty(body.Password))
        return Results.BadRequest("Password cannot be empty");
    var rs = svc.ChangePassword(username, body.Password);
    return Results.Ok(rs);
})
.WithName("dict-change-account-password");

app.MapDelete("/d-acc-by-usn/{username}", (string username) =>
{
    if (string.IsNullOrEmpty(username))
        return Results.BadRequest("Username cannot be empty");
    var rs = svc.DelByUsn(username);
    return Results.Ok(rs);
})
.WithName("dict-delete-account-by-username");

app.MapGet("/d-accs-by-kwd", (string keyword) =>
{
    if (string.IsNullOrEmpty(keyword))
        return Results.BadRequest("Keyword cannot be empty");
    var rs = svc.GetByKwd(keyword);
    return Results.Ok(rs);
})
.WithName("dict-get-accounts-by-keyword");

app.MapGet("/d-acc-by-usn/{username}", (string username) =>
{
    if (string.IsNullOrWhiteSpace(username))
        return Results.BadRequest("Username cannot be empty");
    var rs = svc.IsExist(username);
    return Results.Ok(rs ? "Username exists" : "Username not found");
})
.WithName("dict-get-account-by-username");

app.MapPost("/d-login", (LoginRequest req) =>
{
    if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
        return Results.BadRequest("Username and password are required.");
    var result = svc.Login(req.Username, req.Password);
    if (!result)
        return Results.Unauthorized();
    return Results.Ok("Login successful");
})
.WithName("dict-post-login");

app.Run();

public interface ITimeProvider { DateTime Now { get; } }
public class SystemTimeProvider : ITimeProvider
{
    public DateTime Now => DateTime.Now;
}

public record LoginRequest([property: JsonPropertyName("username")] string Username,
                           [property: JsonPropertyName("password")] string Password);

public record PasswordDto(string Password);
public record AccountCreate(string Username, string Password);