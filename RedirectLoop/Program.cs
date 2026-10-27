using Bandwidth.Standard.Api;
using Bandwidth.Standard.Client;
using Bandwidth.Standard.Model;
using Bandwidth.Standard.Model.Bxml;
using Bandwidth.Standard.Model.Bxml.Verbs;
using Newtonsoft.Json;

//Setting up environment variables
string? BW_ACCOUNT_ID = Environment.GetEnvironmentVariable("BW_ACCOUNT_ID");
string? BW_CLIENT_ID = Environment.GetEnvironmentVariable("BW_CLIENT_ID");
string? BW_CLIENT_SECRET = Environment.GetEnvironmentVariable("BW_CLIENT_SECRET");
string? BASE_CALLBACK_URL = Environment.GetEnvironmentVariable("BASE_CALLBACK_URL");
string? LOCAL_PORT = Environment.GetEnvironmentVariable("LOCAL_PORT");

if (BW_ACCOUNT_ID == null || BW_CLIENT_ID == null || BW_CLIENT_SECRET == null || BASE_CALLBACK_URL == null
    || !int.TryParse(LOCAL_PORT, out int port))
{
    Console.WriteLine("Please set the environmental variables defined in the README");
    Environment.Exit(1);
    return;
}

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls($"http://localhost:{port}");
var app = builder.Build();

Configuration configuration = new Configuration();
configuration.OAuthClientId = BW_CLIENT_ID;
configuration.OAuthClientSecret = BW_CLIENT_SECRET;

var activeCalls = new HashSet<string>();

async Task<Dictionary<string, object>?> ReadBody(HttpContext context)
{
    try
    {
        using var streamReader = new StreamReader(context.Request.Body);
        return JsonConvert.DeserializeObject<Dictionary<string, object>>(await streamReader.ReadToEndAsync());
    }
    catch (JsonException)
    {
        return null;
    }
}

app.MapPost("/callbacks/inbound/voice", async (HttpContext context) =>
{
    var requestBody = await ReadBody(context);
    var eventType = requestBody?.GetValueOrDefault("eventType")?.ToString();
    if (eventType != "initiate" && eventType != "redirect")
    {
        return Results.BadRequest();
    }

    if (eventType == "initiate")
    {
        activeCalls.Add(requestBody!.GetValueOrDefault("callId")?.ToString()!);
    }

    var response = new Response();
    response.Add
    (
        new SpeakSentence()
        {
            Text = "Redirecting call, please wait."
        }
    );
    response.Add
    (
        new Ring()
        {
            Duration = 30
        }
    );
    response.Add
    (
        new Redirect()
        {
            RedirectUrl = "/callbacks/inbound/voice"
        }
    );

    return Results.Content(response.ToBXML(), "application/xml");
});

app.MapPost("/callbacks/inbound/voice/callEnded", async (HttpContext context) =>
{
    var requestBody = await ReadBody(context);
    if (requestBody == null || requestBody.GetValueOrDefault("eventType")?.ToString() != "redirect")
    {
        return Results.BadRequest();
    }

    var response = new Response();
    response.Add
    (
        new SpeakSentence()
        {
            Text = "The call has been ended. Goodbye."
        }
    );
    response.Add
    (
        new Hangup()
    );

    return Results.Content(response.ToBXML(), "application/xml");
});

app.MapPost("/callbacks/inbound/voice/disconnect", async (HttpContext context) =>
{
    var requestBody = await ReadBody(context);
    if (requestBody == null || requestBody.GetValueOrDefault("eventType")?.ToString() != "disconnect")
    {
        return Results.BadRequest();
    }

    activeCalls.Remove(requestBody.GetValueOrDefault("callId")?.ToString() ?? "");
    return Results.NoContent();
});

app.MapGet("/calls", () => Results.Json(activeCalls));

app.MapDelete("/calls/{callId}", async (string callId) =>
{
    if (!activeCalls.Contains(callId))
    {
        return Results.NotFound();
    }

    UpdateCall updateCall = new UpdateCall(
        redirectUrl: BASE_CALLBACK_URL + "/callbacks/inbound/voice/callEnded"
    );

    try
    {
        CallsApi apiInstance = new CallsApi(configuration);
        await apiInstance.UpdateCallAsync(BW_ACCOUNT_ID, callId, updateCall);
    }
    catch (Exception e)
    {
        Console.WriteLine($"Failed to update call: {e.Message}");
        return Results.StatusCode(500);
    }

    activeCalls.Remove(callId);
    return Results.NoContent();
});

app.Run();
