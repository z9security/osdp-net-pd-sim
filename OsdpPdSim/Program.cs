using OSDP.Net.Connections;
using OsdpPdSim;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var osdpPort = app.Configuration.GetValue("Osdp:TcpPort", 9843);
var osdpAddress = app.Configuration.GetValue<byte>("Osdp:Address", 0);

var device = new SimDevice(osdpAddress, app.Services.GetRequiredService<ILoggerFactory>());
var listener = new TcpConnectionListener(osdpPort, 9600, app.Services.GetRequiredService<ILoggerFactory>());

_ = Task.Run(async () =>
{
    app.Logger.LogInformation("Starting OSDP PD listener on TCP port {Port}, address {Address}",
        osdpPort, osdpAddress);
    await device.StartListening(listener);
});

// -- HTTP Control API --

app.MapGet("/status", () => new
{
    connected = device.IsConnected,
    osdpPort,
    osdpAddress
});

app.MapPost("/card-read", (CardReadRequest req) =>
{
    device.QueueCardRead(req.CardNumber, req.BitCount, req.ReaderNumber);
    return Results.Ok(new { queued = true });
});

app.MapPost("/card-read-wiegand26", (Wiegand26Request req) =>
{
    device.QueueWiegand26CardRead(req.FacilityCode, req.CardNumber, req.ReaderNumber);
    return Results.Ok(new { queued = true });
});

app.MapPost("/keypad", (KeypadRequest req) =>
{
    device.QueueKeypad(req.Digits, req.ReaderNumber);
    return Results.Ok(new { queued = true });
});

app.MapPost("/input-status", (InputStatusRequest req) =>
{
    device.SetInputStatuses(req.Inputs);
    return Results.Ok(new { updated = true });
});

app.MapGet("/commands", () => device.ReceivedCommands);

app.MapPost("/reset", () =>
{
    device.ClearCommands();
    return Results.Ok(new { cleared = true });
});

app.Run();

// -- Request DTOs --

record CardReadRequest(int CardNumber, int BitCount = 26, byte ReaderNumber = 0);
record Wiegand26Request(int FacilityCode, int CardNumber, byte ReaderNumber = 0);
record KeypadRequest(string Digits, byte ReaderNumber = 0);
record InputStatusRequest(bool[] Inputs);
