using C2Server.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Mở CORS cho Web React (cổng 5173)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)    // Vẫn giữ để sếp test local
                                                // Cho phép Web chạy trên Port 80 của EC2

               .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });

});

// Thêm SignalR VÀ CẤU HÌNH TĂNG GIỚI HẠN DỮ LIỆU LÊN 10MB
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 10 * 1024 * 1024; // Mở rộng giới hạn nhận lên 10MB
    options.EnableDetailedErrors = true; // Bật thông báo lỗi chi tiết
}).AddMessagePackProtocol();

var app = builder.Build();

app.UseCors("AllowReactApp");
app.MapHub<RemoteHub>("/remoteHub");

app.MapGet("/", () => "C2 Server Hub dang chay thanh cong tren .NET 9!");

app.Run();