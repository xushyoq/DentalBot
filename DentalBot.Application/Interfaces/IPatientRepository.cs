using DentalBot.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace DentalBot.Application.Interfaces
{
    public interface IPatientRepository
    {
        Task<Patient?> GetByIdAsync(int id);
        Task<IEnumerable<Patient>> GetAllAsync();
        Task<IEnumerable<PatientVisit>> GetVisitsByDateAsync(DateTime date);
        Task<IEnumerable<Patient>> SearchAsync(string query, int maxResults);
        Task<int> GetVisitCountAsync(int patientId);
        Task<PatientVisit?> GetLastVisitAsync(int patientId);
        Task<Patient> AddAsync(Patient patient);
        Task<Patient> UpdateAsync(Patient patient);
        Task<Patient> DeleteAsync(Patient patient);
        Task AddPatientWithVisitAsync(Patient patient, int employeeId, DateTime visitDate);
        Task AddVisitAsync(int patientId, int employeeId, DateTime visitDate);
    }
}
