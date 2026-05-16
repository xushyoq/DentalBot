namespace DentalBot.Domain.Entities
{
    public class PatientVisit
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public Patient Patient { get; set; } = null!;
        public int EmployeeId { get; set; }
        public Employee Employee { get; set; } = null!;
        public DateTime VisitDate { get; set; }
    }
}
