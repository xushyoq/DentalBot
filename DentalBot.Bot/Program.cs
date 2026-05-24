using System.Security.Cryptography;
using System.Text;
using DentalBot.Application.Interfaces;
using DentalBot.Application.Services;
using DentalBot.Bot;
using DentalBot.Bot.Handlers;
using DentalBot.Infrastructure.Data;
using DentalBot.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Telegram.Bot;
using Telegram.Bot.Types;

var builder = WebApplication.CreateBuilder(args);

var port = builder.Configuration["PORT"];
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

var connectionString = NormalizeConnectionString(
    builder.Configuration.GetConnectionString("Default")
    ?? builder.Configuration["DATABASE_URL"]);
var token = builder.Configuration["TelegramBot:Token"];

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'Default' is not configured.");
}

if (string.IsNullOrWhiteSpace(token))
{
    throw new InvalidOperationException("Telegram bot token is not configured.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(token));
builder.Services.AddScoped<UpdateHandler>();

builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();

builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<IPatientService, PatientService>();

builder.Services.AddSingleton<UserStateService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

var webhookBaseUrl = app.Configuration["TelegramBot:WebhookUrl"]
    ?? app.Configuration["RENDER_EXTERNAL_URL"];

if (string.IsNullOrWhiteSpace(webhookBaseUrl))
{
    throw new InvalidOperationException("Telegram bot webhook URL is not configured.");
}

var webhookSecret = app.Configuration["TelegramBot:WebhookSecret"];
if (string.IsNullOrWhiteSpace(webhookSecret))
{
    webhookSecret = CreateStableWebhookSecret(token);
}

var webhookPath = $"/telegram/webhook/{webhookSecret}";
var webhookUrl = $"{webhookBaseUrl.TrimEnd('/')}{webhookPath}";

app.MapGet("/health", () => Results.Ok("OK"));

app.MapPost(webhookPath, async (Update update, UpdateHandler handler, CancellationToken ct) =>
{
    await handler.HandleUpdateAsync(update, ct);
    return Results.Ok();
});

await SetTelegramWebhookAsync(token, webhookUrl);

Console.WriteLine("Bot webhook ishga tushdi");

app.Run();

static async Task SetTelegramWebhookAsync(string token, string webhookUrl)
{
    using var httpClient = new HttpClient();
    using var content = new FormUrlEncodedContent(new Dictionary<string, string>
    {
        ["url"] = webhookUrl
    });

    var response = await httpClient.PostAsync($"https://api.telegram.org/bot{token}/setWebhook", content);
    response.EnsureSuccessStatusCode();
}

static string CreateStableWebhookSecret(string token)
{
    var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
    return Convert.ToHexString(hash)[..32].ToLowerInvariant();
}

static string? NormalizeConnectionString(string? connectionString)
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return connectionString;
    }

    if (!Uri.TryCreate(connectionString, UriKind.Absolute, out var databaseUrl)
        || (databaseUrl.Scheme != "postgres" && databaseUrl.Scheme != "postgresql"))
    {
        return connectionString;
    }

    var userInfo = databaseUrl.UserInfo.Split(':', 2);
    var builder = new NpgsqlConnectionStringBuilder
    {
        Host = databaseUrl.Host,
        Port = databaseUrl.Port > 0 ? databaseUrl.Port : 5432,
        Database = Uri.UnescapeDataString(databaseUrl.AbsolutePath.TrimStart('/')),
        Username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : string.Empty,
        Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty
    };

    return builder.ConnectionString;
}
