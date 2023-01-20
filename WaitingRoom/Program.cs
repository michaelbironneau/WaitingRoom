using System.Net;
using Microsoft.AspNetCore.Mvc;


var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
const string port = "8080";

Task.Run(() => AuctionManager.Start());
app.Logger.LogInformation("Auction Manager Started!");

app.MapGet("/backend-resource", (ILogger<Program> logger, [FromHeader(Name = "X-Access-Token")] string? token) =>
{
    if (token == null)
    {
        return Results.Redirect("/queue");
    }
    else
    {
        try
        {
            Access.Validate(token);
            return Results.Text("You're through to the backend resource!");
        }
        catch (InvalidTokenException ex)
        {
            logger.LogWarning(ex.ToString());
            return Results.BadRequest("Invalid Access Token");
        }
    }
});


app.MapGet("/queue", (ILogger<Program> logger, [FromHeader(Name = "X-Queue-Token")] string? tokenStr) =>
{
    try
    {
        if (tokenStr == null)
        {
            return AuctionManager.EnterNew();
        }
        WaitToken token = WaitToken.Parse(tokenStr);
        return AuctionManager.EnterAt(token.QueuePosition);
    }
    catch (InvalidTokenException ex)
    {
        logger.LogWarning(ex.ToString());
        return Results.BadRequest("Invalid Wait Token Signature");
    }
});

app.Logger.LogInformation($"Server listening on port {port}");
app.Run($"http://localhost:{port}");
