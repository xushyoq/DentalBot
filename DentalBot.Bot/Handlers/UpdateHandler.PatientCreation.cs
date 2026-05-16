using DentalBot.Domain.Entities;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace DentalBot.Bot.Handlers
{
    public partial class UpdateHandler
    {
        private void StartPatientCreation(UserState state, long telegramId)
        {
            state.Step = UserStep.WaitingFirstName;
            state.TempPatient = new Patient();
            state.SearchResults.Clear();
            state.SelectedPatientId = null;
            _userStateService.SetState(telegramId, state);
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
                    await AskDoctorForNewPatient(message, state, text, telegramId, ct);
                    break;

                case UserStep.WaitingDoctor:
                    await SavePatient(message, patient, telegramId, text, ct);
                    return;
            }

            _userStateService.SetState(telegramId, state);
        }

        private async Task AskDoctorForNewPatient(Message message, UserState state, string workplace, long telegramId, CancellationToken ct)
        {
            state.TempPatient!.Workplace = workplace;
            state.Step = UserStep.WaitingDoctor;

            var keyboard = await CreateDoctorsKeyboardOrCancel(message.Chat.Id, telegramId, ct);
            if (keyboard == null) return;

            await _botClient.SendMessage(message.Chat.Id, "Shifokorni tanlang:", replyMarkup: keyboard, cancellationToken: ct);
        }

        private async Task SavePatient(Message message, Patient patient, long telegramId, string text, CancellationToken ct)
        {
            if (!TryGetEmployeeId(text, out var doctorId))
            {
                await _botClient.SendMessage(message.Chat.Id, "Shifokorni ro'yxatdagi tugma orqali tanlang:", cancellationToken: ct);
                return;
            }

            await _patientService.AddPatientWithVisitAsync(patient, doctorId, DateTime.UtcNow);
            _userStateService.ClearState(telegramId);
            await _botClient.SendMessage(message.Chat.Id, "✅ Bemor qo'shildi va bugungi tashrif yaratildi.", cancellationToken: ct);
            await ShowMainMenu(message.Chat.Id, ct);
        }

        private async Task<ReplyKeyboardMarkup?> CreateDoctorsKeyboardOrCancel(long chatId, long telegramId, CancellationToken ct)
        {
            var employees = (await _employeeService.GetAllAsync()).ToList();

            if (employees.Count == 0)
            {
                await _botClient.SendMessage(chatId, "⚠️ Bazaga hali shifokorlar kiritilmagan.", cancellationToken: ct);
                _userStateService.ClearState(telegramId);
                await ShowMainMenu(chatId, ct);
                return null;
            }

            var keyboardButtons = employees
                .Select(employee => new[] { new KeyboardButton($"{employee.Id}. {employee.FirstName} {employee.LastName}") })
                .Append(new[] { new KeyboardButton(CancelButton) })
                .ToArray();

            return new ReplyKeyboardMarkup(keyboardButtons)
            {
                ResizeKeyboard = true
            };
        }

        private static bool TryGetEmployeeId(string text, out int employeeId)
        {
            employeeId = 0;
            var separatorIndex = text.IndexOf('.');

            return separatorIndex > 0
                && int.TryParse(text[..separatorIndex], out employeeId);
        }
    }
}
