using DentalBot.Bot;
using DentalBot.Bot.Handlers;
using DentalBot.Infrastructure.Data;
using DentalBot.Application.Interfaces;
using DentalBot.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Telegram.Bot;
using DentalBot.Application.Services;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((context, services) =>
{
    var connectionString = NormalizeConnectionString(
        context.Configuration.GetConnectionString("Default")
        ?? context.Configuration["DATABASE_URL"]);
    var token = context.Configuration["TelegramBot:Token"];

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("Connection string 'Default' is not configured.");
    }

    if (string.IsNullOrWhiteSpace(token))
    {
        throw new InvalidOperationException("Telegram bot token is not configured.");
    }

    services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));

    services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(token));
    services.AddSingleton<UpdateHandler>();
    services.AddHostedService<BotBackgroundService>();

    services.AddScoped<IEmployeeRepository, EmployeeRepository>();
    services.AddScoped<IEmployeeService, EmployeeService>();

    services.AddScoped<IPatientRepository, PatientRepository>();
    services.AddScoped<IPatientService, PatientService>();

    services.AddSingleton<UserStateService>();

});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

app.Run();

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
