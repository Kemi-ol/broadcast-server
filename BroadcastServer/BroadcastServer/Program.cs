using Microsoft.AspNetCore.Builder;
using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);



builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Place UseWebSockets before HTTPS redirection to handle WebSocket upgrade first

app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2),
});


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();


// Configure the WebSocket endpoint
app.Map("/wss", (app) =>
{
    app.Run(async context =>
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            await EchoLoop(webSocket);  // Process incoming WebSocket messages
        }
        else
        {
            context.Response.StatusCode = 400; // Bad Request if it's not a WebSocket request
        }
    });
});

// Handle WebSocket communication
async Task EchoLoop(WebSocket webSocket)
{
    var buffer = new byte[1024 * 4];  // Buffer for receiving WebSocket data

    try
    {
        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Text)
            {
                string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine("Received: " + message);

                // Echo the message back to the client
                byte[] response = Encoding.UTF8.GetBytes($"Echo: {message}");
                await webSocket.SendAsync(new ArraySegment<byte>(response), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            else if (result.MessageType == WebSocketMessageType.Close)
            {
                Console.WriteLine("WebSocket connection closed");
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error: " + ex.Message);
    }
}


app.Run();
