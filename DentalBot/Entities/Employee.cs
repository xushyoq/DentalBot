namespace DentalBot.Domain.Entities
{
    public class Employee
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public long TelegramId { get; set; }
        public ICollection<PatientEmployee> PatientEmployees { get; set; } = new List<PatientEmployee>();
    }
}
