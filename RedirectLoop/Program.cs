using System.Reflection;
using Bandwidth.Standard.Api;
using Bandwidth.Standard.Client;
using Bandwidth.Standard.Model;
using Bandwidth.Standard.Model.Bxml;
using Bandwidth.Standard.Model.Bxml.Verbs;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

string BW_USERNAME;
string BW_PASSWORD;
string BW_ACCOUNT_ID;
string BASE_CALLBACK_URL;


//Setting up environment variables
try
{
    BW_USERNAME = Environment.GetEnvironmentVariable("BW_USERNAME");
    BW_PASSWORD = Environment.GetEnvironmentVariable("BW_PASSWORD");
    BW_ACCOUNT_ID = Environment.GetEnvironmentVariable("BW_ACCOUNT_ID");
    BASE_CALLBACK_URL = Environment.GetEnvironmentVariable("BASE_CALLBACK_URL");
}
catch (Exception)
{
    Console.WriteLine("Please set the environmental variables defined in the README");
    throw;
}

Configuration configuration = new Configuration();
configuration.Username = BW_USERNAME;
configuration.Password = BW_PASSWORD;

List<string> activeCalls = new List<string>();

app.MapPost("/callbacks/inboundCall", async (HttpContext context) =>
{
    var requestBody = new Dictionary<string, string>();
    using(var streamReader = new StreamReader(context.Request.Body))
    {
        var body = await streamReader.ReadToEndAsync();
        requestBody = JsonConvert.DeserializeObject<Dictionary<string,string>>(body);
    }

    var eventType = requestBody["eventType"];

    if (eventType == "initiate")
    {
        activeCalls.Add(requestBody["callId"]);
    }

    if (eventType == "initiate" || eventType == "redirect")
    {
        SpeakSentence unavailableSpeakSentence = new SpeakSentence()
        {
            Text = "Redirecting call, please wait."
        };
        Ring ring = new Ring()
        {
            Duration = 30
        };
        Redirect redirect = new Redirect()
        {
            RedirectUrl =  "/callbacks/inboundCall"
        };
        Response response = new Response(new IVerb[] { unavailableSpeakSentence, ring, redirect });

        return response.ToBXML();
    };
    return "";
});

app.MapPost("/callbacks/callEnded", async (HttpContext context) =>
{
    var requestBody = new Dictionary<string, string>();
    using(var streamReader = new StreamReader(context.Request.Body))
    {
        var body = await streamReader.ReadToEndAsync();
        requestBody = JsonConvert.DeserializeObject<Dictionary<string,string>>(body);
    }

    var eventType = requestBody["eventType"];

    if (eventType == "redirect")
    {
        SpeakSentence speakSentence = new SpeakSentence()
        {
            Text = "The call has been ended. Goodbye"
        };
        Response response = new Response(new IVerb[] { speakSentence });
        activeCalls.Remove(requestBody["callId"]);
        return response.ToBXML();
    };
    return "";
});

app.MapDelete("/calls/{callId}", async (string callId, HttpContext context) =>
{
    if (activeCalls.Contains(callId))
    {
        UpdateCall updateCall = new UpdateCall(
            state: CallStateEnum.Active,
            redirectUrl: BASE_CALLBACK_URL + "/callbacks/callEnded",
            redirectMethod: RedirectMethodEnum.POST
        );
        var apiInstance = new CallsApi(configuration);
        await apiInstance.UpdateCallAsync(BW_ACCOUNT_ID, callId, updateCall);
        activeCalls.Remove(callId);
        
        return $"call {callId} will be ended";
    }
    else
    {
        Results.NotFound();
        return "call not found";
    }
});

app.MapGet("/calls", (HttpContext context) =>
{
    return activeCalls;
});

app.Run();
