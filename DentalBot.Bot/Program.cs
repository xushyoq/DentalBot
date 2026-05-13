using DentalBot.Bot;
using DentalBot.Bot.Handlers;
using DentalBot.Infrastructure.Data;
using DentalBot.Application.Interfaces;
using DentalBot.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using DentalBot.Application.Services;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((context, services) =>
{
    var connectionString = context.Configuration.GetConnectionString("Default");
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

builder.Build().Run();
