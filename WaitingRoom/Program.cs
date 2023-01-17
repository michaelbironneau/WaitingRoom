using System.Net;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

AuctionManager.Start();

app.MapGet("/backend-resource", ([FromHeader(Name = "X-Access-Token")] string token) =>
{
    if (token.Length == 0)
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
            Console.WriteLine(ex.ToString());
            return Results.BadRequest("Invalid Access Token");
        }
    }
});


app.MapGet("/queue", ([FromHeader(Name = "X-Queue-Token")] string tokenStr) =>
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
    catch (InvalidTokenException ex)
    {
        Console.WriteLine(ex.ToString());
        return Results.BadRequest("Invalid Wait Token Signature");
    }
});

app.Run();
