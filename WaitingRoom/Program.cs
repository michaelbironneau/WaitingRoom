using System.Net;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

AuctionManager.Start();

// TODO: Validate access token and redirect to /queue if invalid
app.MapGet("/backend-resource", () => "You're through!");


app.MapPost("/queue", ([FromBody] string tokenStr) =>
{
    try
    {
        if (tokenStr.Length == 0)
        {
            return AuctionManager.EnterNew();
        }
        WaitToken token = WaitToken.Parse(tokenStr);
        return AuctionManager.EnterAt(token.QueuePosition);
    }
    catch (InvalidTokenException)
    {
        return Results.BadRequest("Invalid Wait Token Signature");
    }
});

app.Run();
