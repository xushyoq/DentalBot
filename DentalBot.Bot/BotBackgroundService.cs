using DentalBot.Bot.Handlers;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace DentalBot.Bot
{
    public class BotBackgroundService : BackgroundService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly UpdateHandler _handler;

        public BotBackgroundService(ITelegramBotClient botClient, UpdateHandler handler)
        {
            _botClient = botClient;
            _handler = handler;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("Бот запущен");

            _botClient.StartReceiving(
                updateHandler: (bot, update, ct) => _handler.HandleUpdateAsync(update, ct),
                errorHandler: (bot, ex, ct) => _handler.HandleErrorAsync(ex, ct),
                receiverOptions: new ReceiverOptions { AllowedUpdates = [] },
                cancellationToken: stoppingToken);
            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Работа бота остановлена");
            }
            
        }
    }
}