using DentalBot.Application.Interfaces;
using DentalBot.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace DentalBot.Application.Services
{
    public class PatientService : IPatientService
    {
        private readonly IPatientRepository _patientRepository;

        public PatientService(IPatientRepository patientRepository)
        {
            _patientRepository = patientRepository;
        }

        public async Task<Patient> AddAsync(Patient patient)
        {
            return await _patientRepository.AddAsync(patient);
        }

        public async Task<Patient> DeleteAsync(int id)
        {
            var patient = await _patientRepository.GetByIdAsync(id);
            if (patient == null)
            {
                throw new InvalidOperationException($"Patient with id {id} was not found.");
            }

            return await _patientRepository.DeleteAsync(patient);
        }

        public async Task<IEnumerable<Patient>> GetAllAsync()
        {
            return await _patientRepository.GetAllAsync();
        }

        public async Task<IEnumerable<Patient>> GetByVisitDateAsync(DateTime date)
        {
            return await _patientRepository.GetByVisitDateAsync(date);
        }

        public async Task<Patient?> GetByIdAsync(int id)
        {
            return await _patientRepository.GetByIdAsync(id);
        }

        public async Task<Patient> UpdateAsync(Patient patient)
        {
            return await _patientRepository.UpdateAsync(patient);
        }

        public async Task AddPatientWithEmployeeAsync(Patient patient, int employeeId)
        {
            await _patientRepository.AddPatientWithEmployeeAsync(patient, employeeId);
        }
    }
}
