namespace DentalBot.Domain.Entities
{
    public class Patient
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public int BirthYear { get; set; }
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Workplace { get; set; } = string.Empty;
        public ICollection<PatientEmployee> PatientEmployees { get; set; } = new List<PatientEmployee>();
        public ICollection<PatientVisit> Visits { get; set; } = new List<PatientVisit>();

    }
}
