using DentalBot.Application.Interfaces;
using DentalBot.Domain.Entities;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DentalBot.Bot.Handlers
{
    public partial class UpdateHandler
    {
        private const int MaxSearchResults = 10;
        private const string AddVisitButton = "🦷 Tashrif qo'shish";
        private const string AddPatientButton = "➕ Bemor qo'shish";
        private const string SearchPatientButton = "🔍 Bemor qidirish";
        private const string TodayListButton = "📋 Bugungi ro'yxat";
        private const string ExportToExcelButton = "📤 Excelga eksport";
        private const string CancelButton = "🚫 Bekor qilish";

        private readonly ITelegramBotClient _botClient;
        private readonly IEmployeeService _employeeService;
        private readonly IPatientService _patientService;
        private readonly UserStateService _userStateService;

        public UpdateHandler(ITelegramBotClient botClient, IEmployeeService employeeService, IPatientService patientService, UserStateService stateService)
        {
            _botClient = botClient;
            _employeeService = employeeService;
            _patientService = patientService;
            _userStateService = stateService;
        }

        public async Task HandleUpdateAsync(Update update, CancellationToken ct)
        {
            if (update.Message?.Text == null || update.Message.From == null) return;

            var message = update.Message;
            var telegramId = message.From.Id;

            var authResult = await _employeeService.AuthOrRegisterFirstUserAsync(
                telegramId,
                message.From.FirstName,
                message.From.LastName ?? string.Empty);

            if (!authResult.IsAuthenticated)
            {
                await _botClient.SendMessage(
                    message.Chat.Id,
                    "⛔ Sizda bu botdan foydalanish huquqi yo'q.",
                    cancellationToken: ct);
                return;
            }

            if (authResult.WasCreated)
            {
                await _botClient.SendMessage(
                    message.Chat.Id,
                    "✅ Siz birinchi foydalanuvchi sifatida avtomatik ro'yxatdan o'tdingiz.",
                    cancellationToken: ct);
            }

            var state = _userStateService.GetState(telegramId);

            if (message.Text == CancelButton)
            {
                _userStateService.ClearState(telegramId);
                await _botClient.SendMessage(message.Chat.Id, "🚫 Amal bekor qilindi.", cancellationToken: ct);
                await ShowMainMenu(message.Chat.Id, ct);
                return;
            }

            if (state.Step != UserStep.None)
            {
                await HandleStateInput(message, state, telegramId, ct);
                return;
            }

            if (message.Text == "/start")
            {
                await ShowMainMenu(message.Chat.Id, ct);
                return;
            }

            if (message.Text == AddVisitButton)
            {
                state.Step = UserStep.WaitingVisitSearch;
                state.SearchResults.Clear();
                state.SelectedPatientId = null;
                _userStateService.SetState(telegramId, state);
                await _botClient.SendMessage(message.Chat.Id, "Bemor ismi, familiyasi yoki telefon raqamini kiriting:", replyMarkup: CreateCancelKeyboard(), cancellationToken: ct);
                return;
            }

            if (message.Text == AddPatientButton)
            {
                StartPatientCreation(state, telegramId);
                await _botClient.SendMessage(message.Chat.Id, "Bemor ismini kiriting:", replyMarkup: CreateCancelKeyboard(), cancellationToken: ct);
                return;
            }

            if (message.Text == SearchPatientButton)
            {
                state.Step = UserStep.WaitingPatientSearch;
                state.SearchResults.Clear();
                _userStateService.SetState(telegramId, state);
                await _botClient.SendMessage(message.Chat.Id, "Bemor ismi, familiyasi yoki telefon raqamini kiriting:", replyMarkup: CreateCancelKeyboard(), cancellationToken: ct);
                return;
            }

            if (message.Text == TodayListButton)
            {
                await ShowTodayPatients(message.Chat.Id, ct);
                return;
            }
        }

        public Task HandleErrorAsync(Exception exception, CancellationToken ct)
        {
            Console.WriteLine($"Xatolik: {exception.Message}");
            return Task.CompletedTask;
        }

        private async Task HandleStateInput(Message message, UserState state, long telegramId, CancellationToken ct)
        {
            switch (state.Step)
            {
                case UserStep.WaitingFirstName:
                case UserStep.WaitingLastName:
                case UserStep.WaitingBirthYear:
                case UserStep.WaitingPhone:
                case UserStep.WaitingAddress:
                case UserStep.WaitingWorkplace:
                case UserStep.WaitingDoctor:
                    await HandlePatientInput(message, state, telegramId, ct);
                    break;

                case UserStep.WaitingPatientSearch:
                    await HandlePatientSearch(message, state, telegramId, ct);
                    break;

                case UserStep.WaitingPatientSelection:
                    await HandlePatientSelection(message, state, telegramId, ct);
                    break;

                case UserStep.WaitingVisitSearch:
                    await HandleVisitSearch(message, state, telegramId, ct);
                    break;

                case UserStep.WaitingVisitSelection:
                    await HandleVisitSelection(message, state, telegramId, ct);
                    break;

                case UserStep.WaitingVisitDoctor:
                    await HandleVisitDoctor(message, state, telegramId, ct);
                    break;
            }
        }
    }
}
