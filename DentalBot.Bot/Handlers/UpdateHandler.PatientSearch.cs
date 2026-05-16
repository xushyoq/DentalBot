using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DentalBot.Bot.Handlers
{
    public partial class UpdateHandler
    {
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

            await SendSearchResults(message.Chat.Id, patients, "Bemor kartasini ochish uchun raqamini yuboring.", ct);
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

        private async Task HandleVisitSearch(Message message, UserState state, long telegramId, CancellationToken ct)
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
                await _botClient.SendMessage(message.Chat.Id, "🔍 Bemor topilmadi. Boshqa so'rov kiriting yoki amalni bekor qiling:", replyMarkup: CreateCancelKeyboard(), cancellationToken: ct);
                return;
            }

            state.SearchResults = patients;
            state.Step = UserStep.WaitingVisitSelection;
            _userStateService.SetState(telegramId, state);

            await SendSearchResults(message.Chat.Id, patients, "Tashrif qo'shish uchun bemor raqamini yuboring.", ct);
        }

        private async Task HandleVisitSelection(Message message, UserState state, long telegramId, CancellationToken ct)
        {
            if (!int.TryParse(message.Text, out var selectedNumber) ||
                selectedNumber < 1 ||
                selectedNumber > state.SearchResults.Count)
            {
                await _botClient.SendMessage(message.Chat.Id, "Ro'yxatdan bemor raqamini kiriting:", replyMarkup: CreateCancelKeyboard(), cancellationToken: ct);
                return;
            }

            var patient = state.SearchResults[selectedNumber - 1];
            state.SelectedPatientId = patient.Id;
            state.Step = UserStep.WaitingVisitDoctor;
            _userStateService.SetState(telegramId, state);

            await ShowPatientCard(message.Chat.Id, patient, ct);

            var keyboard = await CreateDoctorsKeyboardOrCancel(message.Chat.Id, telegramId, ct);
            if (keyboard == null) return;

            await _botClient.SendMessage(message.Chat.Id, "Bugungi tashrif uchun shifokorni tanlang:", replyMarkup: keyboard, cancellationToken: ct);
        }

        private async Task HandleVisitDoctor(Message message, UserState state, long telegramId, CancellationToken ct)
        {
            if (state.SelectedPatientId == null)
            {
                _userStateService.ClearState(telegramId);
                await _botClient.SendMessage(message.Chat.Id, "Bemor tanlanmagan. Amal bekor qilindi.", cancellationToken: ct);
                await ShowMainMenu(message.Chat.Id, ct);
                return;
            }

            if (!TryGetEmployeeId(message.Text!, out var doctorId))
            {
                await _botClient.SendMessage(message.Chat.Id, "Shifokorni ro'yxatdagi tugma orqali tanlang:", cancellationToken: ct);
                return;
            }

            await _patientService.AddVisitAsync(state.SelectedPatientId.Value, doctorId, DateTime.UtcNow);
            _userStateService.ClearState(telegramId);
            await _botClient.SendMessage(message.Chat.Id, "✅ Bugungi tashrif qo'shildi.", cancellationToken: ct);
            await ShowMainMenu(message.Chat.Id, ct);
        }

        private async Task SendSearchResults(long chatId, List<DentalBot.Domain.Entities.Patient> patients, string instruction, CancellationToken ct)
        {
            var response = new StringBuilder();
            response.AppendLine("🔍 Topilgan bemorlar:");
            response.AppendLine();

            for (var i = 0; i < patients.Count; i++)
            {
                var patient = patients[i];
                response.AppendLine($"{i + 1}. {patient.FirstName} {patient.LastName}, {patient.BirthYear}");
                response.AppendLine($"   📞 {patient.Phone}");
                response.AppendLine();
            }

            response.AppendLine(instruction);

            await _botClient.SendMessage(chatId, response.ToString(), replyMarkup: CreateCancelKeyboard(), cancellationToken: ct);
        }
    }
}
