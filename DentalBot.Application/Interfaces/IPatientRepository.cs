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
        Task<Patient> AddAsync(Patient patient);
        Task<Patient> UpdateAsync(Patient patient);
        Task<Patient> DeleteAsync(Patient patient);
        Task AddPatientWithEmployeeAsync(Patient patient, int employeeId);
    }
}
