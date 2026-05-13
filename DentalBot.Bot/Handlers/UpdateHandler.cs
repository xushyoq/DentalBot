using DentalBot.Application.Interfaces;
using DentalBot.Domain.Entities;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace DentalBot.Bot.Handlers
{
    public class UpdateHandler
    {
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

            var isAuthenticated = await _employeeService.AuthService(telegramId);

            if (!isAuthenticated)
            {
                await _botClient.SendMessage(
                    message.Chat.Id,
                    "Вы не авторизованы для использования этого бота.",
                    cancellationToken: ct);
                return;
            }

            var state = _userStateService.GetState(telegramId);

            if (state.Step != UserStep.None)
            {
                await HandlePatientInput(message, state, telegramId, ct);
                return;
            }

            if (message.Text == "/start")
            {
                await ShowMainMenu(message.Chat.Id, ct);
                return;
            }

            if (message.Text == "➕ Добавить пациента")
            {
                state.Step = UserStep.WaitingFirstName;
                state.TempPatient = new Patient();
                _userStateService.SetState(telegramId, state);
                await _botClient.SendMessage(message.Chat.Id, "Введите имя пациента:", cancellationToken: ct);
                return;
            }
        }

        private async Task HandlePatientInput(Message message, UserState state, long telegramId, CancellationToken ct)
        {
            var text = message.Text!;
            var patient = state.TempPatient!;

            switch (state.Step)
            {
                case UserStep.WaitingFirstName:
                    patient.FirstName = text;
                    state.Step = UserStep.WaitingLastName;
                    await _botClient.SendMessage(message.Chat.Id, "Введите фамилию:", cancellationToken: ct);
                    break;

                case UserStep.WaitingLastName:
                    patient.LastName = text;
                    state.Step = UserStep.WaitingBirthYear;
                    await _botClient.SendMessage(message.Chat.Id, "Введите год рождения:", cancellationToken: ct);
                    break;

                case UserStep.WaitingBirthYear:
                    if (!int.TryParse(text, out int year))
                    {
                        await _botClient.SendMessage(message.Chat.Id, "Введите корректный год (например 1990):", cancellationToken: ct);
                        return;
                    }

                    patient.BirthYear = year;
                    state.Step = UserStep.WaitingPhone;
                    await _botClient.SendMessage(message.Chat.Id, "Введите номер телефона:", cancellationToken: ct);
                    break;

                case UserStep.WaitingPhone:
                    patient.Phone = text;
                    state.Step = UserStep.WaitingAddress;
                    await _botClient.SendMessage(message.Chat.Id, "Введите адрес:", cancellationToken: ct);
                    break;

                case UserStep.WaitingAddress:
                    patient.Address = text;
                    state.Step = UserStep.WaitingWorkplace;
                    await _botClient.SendMessage(message.Chat.Id, "Введите место работы:", cancellationToken: ct);
                    break;

                case UserStep.WaitingWorkplace:
                    patient.Workplace = text;
                    state.Step = UserStep.WaitingDoctor;

                    var employees = (await _employeeService.GetAllAsync()).ToList();
                    var keyboardButtons = employees
                        .Select(employee => new[] { new KeyboardButton($"{employee.Id}. {employee.FirstName} {employee.LastName}") })
                        .ToArray();

                    if (keyboardButtons.Length == 0)
                    {
                        await _botClient.SendMessage(message.Chat.Id, "В базе пока нет врачей.", cancellationToken: ct);
                        _userStateService.ClearState(telegramId);
                        await ShowMainMenu(message.Chat.Id, ct);
                        return;
                    }

                    var keyboard = new ReplyKeyboardMarkup(keyboardButtons)
                    {
                        ResizeKeyboard = true
                    };

                    await _botClient.SendMessage(message.Chat.Id, "Выберите врача:", replyMarkup: keyboard, cancellationToken: ct);
                    break;

                case UserStep.WaitingDoctor:
                    if (!TryGetEmployeeId(text, out var doctorId))
                    {
                        await _botClient.SendMessage(message.Chat.Id, "Выберите врача кнопкой из списка:", cancellationToken: ct);
                        return;
                    }

                    patient.VisitDate = DateTime.UtcNow;

                    await _patientService.AddPatientWithEmployeeAsync(patient, doctorId);
                    _userStateService.ClearState(telegramId);
                    await _botClient.SendMessage(message.Chat.Id, "✅ Пациент добавлен!", cancellationToken: ct);
                    await ShowMainMenu(message.Chat.Id, ct);
                    return;
            }

            _userStateService.SetState(telegramId, state);
        }

        public Task HandleErrorAsync(Exception exception, CancellationToken ct)
        {
            Console.WriteLine($"Ошибка: {exception.Message}");
            return Task.CompletedTask;
        }

        private static bool TryGetEmployeeId(string text, out int employeeId)
        {
            employeeId = 0;
            var separatorIndex = text.IndexOf('.');

            return separatorIndex > 0
                && int.TryParse(text[..separatorIndex], out employeeId);
        }

        private async Task ShowMainMenu(long chatId, CancellationToken ct)
        {
            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton("➕ Добавить пациента"), new KeyboardButton("🔍 Поиск пациента") },
                new[] { new KeyboardButton("📋 Список за сегодня"), new KeyboardButton("📤 Экспорт в Excel") },
            })
            {
                ResizeKeyboard = true
            };

            await _botClient.SendMessage(
                chatId,
                "Главное меню:",
                replyMarkup: keyboard,
                cancellationToken: ct);
        }
    }
}
