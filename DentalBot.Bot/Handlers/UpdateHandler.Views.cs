using System.Text;
using DentalBot.Domain.Entities;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace DentalBot.Bot.Handlers
{
    public partial class UpdateHandler
    {
        private async Task ShowTodayPatients(long chatId, CancellationToken ct)
        {
            var visits = (await _patientService.GetVisitsByDateAsync(GetTashkentNow())).ToList();

            if (visits.Count == 0)
            {
                await _botClient.SendMessage(chatId, "📋 Bugun tashriflar ro'yxati bo'sh.", cancellationToken: ct);
                return;
            }

            var message = new StringBuilder();
            message.AppendLine("📋 Bugungi tashriflar:");
            message.AppendLine();

            for (var i = 0; i < visits.Count; i++)
            {
                var visit = visits[i];
                var patient = visit.Patient;
                var visitDate = TimeZoneInfo.ConvertTimeFromUtc(visit.VisitDate, GetTashkentTimeZone());

                message.AppendLine($"{i + 1}. {patient.FirstName} {patient.LastName}, {patient.BirthYear}");
                message.AppendLine($"   📞 {patient.Phone}");
                message.AppendLine($"   👨‍⚕️ {visit.Employee.FirstName} {visit.Employee.LastName}");
                message.AppendLine($"   🕒 {visitDate:HH:mm}");
                message.AppendLine($"   📍 {patient.Address}");
                message.AppendLine();
            }

            await _botClient.SendMessage(chatId, message.ToString(), cancellationToken: ct);
        }

        private async Task ShowPatientCard(long chatId, Patient patient, CancellationToken ct)
        {
            var message = new StringBuilder();
            var visitCount = await _patientService.GetVisitCountAsync(patient.Id);
            var lastVisit = await _patientService.GetLastVisitAsync(patient.Id);

            message.AppendLine("👤 Bemor kartasi");
            message.AppendLine();
            message.AppendLine($"Ism: {patient.FirstName}");
            message.AppendLine($"Familiya: {patient.LastName}");
            message.AppendLine($"Tug'ilgan yili: {patient.BirthYear}");
            message.AppendLine($"Telefon: {patient.Phone}");
            message.AppendLine($"Manzil: {patient.Address}");
            message.AppendLine($"Ish joyi: {patient.Workplace}");
            message.AppendLine();
            message.AppendLine($"🔁 Jami tashriflar: {visitCount}");

            if (lastVisit != null)
            {
                var lastVisitDate = TimeZoneInfo.ConvertTimeFromUtc(lastVisit.VisitDate, GetTashkentTimeZone());
                message.AppendLine($"📅 Oxirgi tashrif: {lastVisitDate:dd.MM.yyyy HH:mm}");
                message.AppendLine($"👨‍⚕️ Oxirgi shifokor: {lastVisit.Employee.FirstName} {lastVisit.Employee.LastName}");
            }

            await _botClient.SendMessage(chatId, message.ToString(), cancellationToken: ct);
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
                new[] { new KeyboardButton(AddVisitButton), new KeyboardButton(AddPatientButton) },
                new[] { new KeyboardButton(SearchPatientButton), new KeyboardButton(TodayListButton) },
                new[] { new KeyboardButton(ExportToExcelButton), new KeyboardButton(CancelButton) },
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

        private static DateTime GetTashkentNow()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, GetTashkentTimeZone());
        }

        private static TimeZoneInfo GetTashkentTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Asia/Tashkent");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("West Asia Standard Time");
            }
        }
    }
}
