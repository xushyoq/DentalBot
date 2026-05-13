namespace DentalBot.Domain.Entities
{
    public class PatientEmployee
    {
        public int PatientId { get; set; }
        public Patient Patient { get; set; } = null!;
        public int EmployeeId { get; set; }
        public Employee Employee { get; set; } = null!;
    }
}
