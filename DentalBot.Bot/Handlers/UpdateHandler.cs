using System.Text;
using DentalBot.Application.Interfaces;
using DentalBot.Domain.Entities;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace DentalBot.Bot.Handlers
{
    public class UpdateHandler
    {
        private const int MaxSearchResults = 10;
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

            var isAuthenticated = await _employeeService.AuthService(telegramId);

            if (!isAuthenticated)
            {
                await _botClient.SendMessage(
                    message.Chat.Id,
                    "⛔ Sizda bu botdan foydalanish huquqi yo'q.",
                    cancellationToken: ct);
                return;
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

            if (message.Text == AddPatientButton)
            {
                state.Step = UserStep.WaitingFirstName;
                state.TempPatient = new Patient();
                _userStateService.SetState(telegramId, state);
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
                    await _botClient.SendMessage(message.Chat.Id, "Bemor familiyasini kiriting:", replyMarkup: CreateCancelKeyboard(), cancellationToken: ct);
                    break;

                case UserStep.WaitingLastName:
                    patient.LastName = text;
                    state.Step = UserStep.WaitingBirthYear;
                    await _botClient.SendMessage(message.Chat.Id, "Tug'ilgan yilini kiriting:", replyMarkup: CreateCancelKeyboard(), cancellationToken: ct);
                    break;

                case UserStep.WaitingBirthYear:
                    if (!int.TryParse(text, out int year))
                    {
                        await _botClient.SendMessage(message.Chat.Id, "To'g'ri yil kiriting, masalan 1990:", replyMarkup: CreateCancelKeyboard(), cancellationToken: ct);
                        return;
                    }

                    patient.BirthYear = year;
                    state.Step = UserStep.WaitingPhone;
                    await _botClient.SendMessage(message.Chat.Id, "Telefon raqamini kiriting:", replyMarkup: CreateCancelKeyboard(), cancellationToken: ct);
                    break;

                case UserStep.WaitingPhone:
                    patient.Phone = text;
                    state.Step = UserStep.WaitingAddress;
                    await _botClient.SendMessage(message.Chat.Id, "Manzilni kiriting:", replyMarkup: CreateCancelKeyboard(), cancellationToken: ct);
                    break;

                case UserStep.WaitingAddress:
                    patient.Address = text;
                    state.Step = UserStep.WaitingWorkplace;
                    await _botClient.SendMessage(message.Chat.Id, "Ish joyini kiriting:", replyMarkup: CreateCancelKeyboard(), cancellationToken: ct);
                    break;

                case UserStep.WaitingWorkplace:
                    patient.Workplace = text;
                    state.Step = UserStep.WaitingDoctor;

                    var employees = (await _employeeService.GetAllAsync()).ToList();
                    var keyboardButtons = employees
                        .Select(employee => new[] { new KeyboardButton($"{employee.Id}. {employee.FirstName} {employee.LastName}") })
                        .Append(new[] { new KeyboardButton(CancelButton) })
                        .ToArray();

                    if (employees.Count == 0)
                    {
                        await _botClient.SendMessage(message.Chat.Id, "⚠️ Bazaga hali shifokorlar kiritilmagan.", cancellationToken: ct);
                        _userStateService.ClearState(telegramId);
                        await ShowMainMenu(message.Chat.Id, ct);
                        return;
                    }

                    var keyboard = new ReplyKeyboardMarkup(keyboardButtons)
                    {
                        ResizeKeyboard = true
                    };

                    await _botClient.SendMessage(message.Chat.Id, "Shifokorni tanlang:", replyMarkup: keyboard, cancellationToken: ct);
                    break;

                case UserStep.WaitingDoctor:
                    if (!TryGetEmployeeId(text, out var doctorId))
                    {
                        await _botClient.SendMessage(message.Chat.Id, "Shifokorni ro'yxatdagi tugma orqali tanlang:", cancellationToken: ct);
                        return;
                    }

                    patient.VisitDate = DateTime.UtcNow;

                    await _patientService.AddPatientWithEmployeeAsync(patient, doctorId);
                    _userStateService.ClearState(telegramId);
                    await _botClient.SendMessage(message.Chat.Id, "✅ Bemor qo'shildi.", cancellationToken: ct);
                    await ShowMainMenu(message.Chat.Id, ct);
                    return;
            }

            _userStateService.SetState(telegramId, state);
        }

        private async Task HandlePatientSearch(Message message, UserState state, long telegramId, CancellationToken ct)
        {
            var query = message.Text!.Trim();

            if (query.Length < 2)
            {
                await _botClient.SendMessage(message.Chat.Id, "Qidirish uchun kamida 2 ta belgi kiriting:", replyMarkup: CreateCancelKeyboard(), cancellationToken: ct);
                return;
            }

            var patients = (await _patientService.SearchAsync(query, MaxSearchResults)).ToList();

            if (patients.Count == 0)
            {
                await _botClient.SendMessage(message.Chat.Id, "🔍 Bemor topilmadi. Boshqa so'rov kiriting:", replyMarkup: CreateCancelKeyboard(), cancellationToken: ct);
                return;
            }

            state.SearchResults = patients;
            state.Step = UserStep.WaitingPatientSelection;
            _userStateService.SetState(telegramId, state);

            var response = new StringBuilder();
            response.AppendLine("🔍 Topilgan bemorlar:");
            response.AppendLine();

            for (var i = 0; i < patients.Count; i++)
            {
                var patient = patients[i];
                response.AppendLine($"{i + 1}. {patient.FirstName} {patient.LastName}, {patient.BirthYear}");
                response.AppendLine($"   📞 {patient.Phone}");
                response.AppendLine($"   📅 {patient.VisitDate:dd.MM.yyyy}");
                response.AppendLine();
            }

            response.AppendLine("Bemor kartasini ochish uchun raqamini yuboring.");

            await _botClient.SendMessage(message.Chat.Id, response.ToString(), replyMarkup: CreateCancelKeyboard(), cancellationToken: ct);
        }

        private async Task HandlePatientSelection(Message message, UserState state, long telegramId, CancellationToken ct)
        {
            if (!int.TryParse(message.Text, out var selectedNumber) ||
                selectedNumber < 1 ||
                selectedNumber > state.SearchResults.Count)
            {
                await _botClient.SendMessage(message.Chat.Id, "Ro'yxatdan bemor raqamini kiriting:", replyMarkup: CreateCancelKeyboard(), cancellationToken: ct);
                return;
            }

            var patient = state.SearchResults[selectedNumber - 1];
            _userStateService.ClearState(telegramId);

            await ShowPatientCard(message.Chat.Id, patient, ct);
            await ShowMainMenu(message.Chat.Id, ct);
        }

        public Task HandleErrorAsync(Exception exception, CancellationToken ct)
        {
            Console.WriteLine($"Xatolik: {exception.Message}");
            return Task.CompletedTask;
        }

        private async Task ShowTodayPatients(long chatId, CancellationToken ct)
        {
            var patients = (await _patientService.GetByVisitDateAsync(DateTime.UtcNow)).ToList();

            if (patients.Count == 0)
            {
                await _botClient.SendMessage(chatId, "📋 Bugun bemorlar ro'yxati bo'sh.", cancellationToken: ct);
                return;
            }

            var message = new StringBuilder();
            message.AppendLine("📋 Bugungi bemorlar:");
            message.AppendLine();

            for (var i = 0; i < patients.Count; i++)
            {
                var patient = patients[i];
                message.AppendLine($"{i + 1}. {patient.FirstName} {patient.LastName}, {patient.BirthYear}");
                message.AppendLine($"   📞 {patient.Phone}");
                message.AppendLine($"   📍 {patient.Address}");
                message.AppendLine($"   🏢 {patient.Workplace}");
                message.AppendLine();
            }

            await _botClient.SendMessage(chatId, message.ToString(), cancellationToken: ct);
        }

        private async Task ShowPatientCard(long chatId, Patient patient, CancellationToken ct)
        {
            var message = new StringBuilder();
            message.AppendLine("👤 Bemor kartasi");
            message.AppendLine();
            message.AppendLine($"Ism: {patient.FirstName}");
            message.AppendLine($"Familiya: {patient.LastName}");
            message.AppendLine($"Tug'ilgan yili: {patient.BirthYear}");
            message.AppendLine($"Telefon: {patient.Phone}");
            message.AppendLine($"Manzil: {patient.Address}");
            message.AppendLine($"Ish joyi: {patient.Workplace}");
            message.AppendLine($"Tashrif sanasi: {patient.VisitDate:dd.MM.yyyy}");

            await _botClient.SendMessage(chatId, message.ToString(), cancellationToken: ct);
        }

        private static bool TryGetEmployeeId(string text, out int employeeId)
        {
            employeeId = 0;
            var separatorIndex = text.IndexOf('.');

            return separatorIndex > 0
                && int.TryParse(text[..separatorIndex], out employeeId);
        }

        private static ReplyKeyboardMarkup CreateCancelKeyboard()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton(CancelButton) }
            })
            {
                ResizeKeyboard = true
            };
        }

        private async Task ShowMainMenu(long chatId, CancellationToken ct)
        {
            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton(AddPatientButton), new KeyboardButton(SearchPatientButton) },
                new[] { new KeyboardButton(TodayListButton), new KeyboardButton(ExportToExcelButton) },
                new[] { new KeyboardButton(CancelButton) },
            })
            {
                ResizeKeyboard = true
            };

            await _botClient.SendMessage(
                chatId,
                "📌 Asosiy menyu:",
                replyMarkup: keyboard,
                cancellationToken: ct);
        }
    }
}
