using C2Server.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Mở CORS cho Web React (cổng 5173)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Thêm SignalR
builder.Services.AddSignalR().AddMessagePackProtocol();

var app = builder.Build();

app.UseCors("AllowReactApp");
app.MapHub<RemoteHub>("/remoteHub");

app.MapGet("/", () => "C2 Server Hub dang chay thanh cong tren .NET 9!");

app.Run();