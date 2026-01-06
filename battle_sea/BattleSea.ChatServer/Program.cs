using BattleSea.ChatServer.Hubs;

var builder = WebApplication.CreateBuilder(args);

// 1. Добавляем SignalR
builder.Services.AddSignalR();

// 2. Настраиваем CORS (разрешаем всем подключаться)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyHeader()          // Разрешаем любые заголовки
              .AllowAnyMethod()          // Разрешаем любые методы (GET, POST и т.д.)
              .SetIsOriginAllowed(_ => true)  // Разрешаем все источники
              .AllowCredentials();       // Разрешаем куки и аутентификацию
    });
});

var app = builder.Build();

// 3. Используем CORS
app.UseCors("AllowAll");

// 4. Включаем маршрутизацию
app.UseRouting();

// 5. Настраиваем эндпоинты
app.UseEndpoints(endpoints =>
{
    // SignalR хаб
    endpoints.MapHub<ChatHub>("/chathub");

    // Простая проверочная страница
    endpoints.MapGet("/", async context =>
    {
        await context.Response.WriteAsync("🚀 Сервер чата запущен!\n");
        await context.Response.WriteAsync($"📡 HTTP: http://localhost:\n");
        await context.Response.WriteAsync($"🔐 HTTPS: https://localhost:\n");
        await context.Response.WriteAsync($"💬 SignalR Hub: /chathub\n");
    });
});

// 6. Запускаем приложение
app.Run();