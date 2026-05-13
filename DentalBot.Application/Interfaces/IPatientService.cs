using DentalBot.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace DentalBot.Application.Interfaces
{
    public interface IPatientService
    {
        Task<Patient?> GetByIdAsync(int id);
        Task<IEnumerable<Patient>> GetAllAsync();
        Task<Patient> AddAsync(Patient patient);
        Task<Patient> UpdateAsync(Patient patient);
        Task<Patient> DeleteAsync(int id);
        Task AddPatientWithEmployeeAsync(Patient patient, int employeeId);
    }
}
