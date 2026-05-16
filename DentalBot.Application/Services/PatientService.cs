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

        public async Task<IEnumerable<PatientVisit>> GetVisitsByDateAsync(DateTime date)
        {
            return await _patientRepository.GetVisitsByDateAsync(date);
        }

        public async Task<IEnumerable<Patient>> SearchAsync(string query, int maxResults)
        {
            return await _patientRepository.SearchAsync(query, maxResults);
        }

        public async Task<int> GetVisitCountAsync(int patientId)
        {
            return await _patientRepository.GetVisitCountAsync(patientId);
        }

        public async Task<PatientVisit?> GetLastVisitAsync(int patientId)
        {
            return await _patientRepository.GetLastVisitAsync(patientId);
        }

        public async Task<Patient?> GetByIdAsync(int id)
        {
            return await _patientRepository.GetByIdAsync(id);
        }

        public async Task<Patient> UpdateAsync(Patient patient)
        {
            return await _patientRepository.UpdateAsync(patient);
        }

        public async Task AddPatientWithVisitAsync(Patient patient, int employeeId, DateTime visitDate)
        {
            await _patientRepository.AddPatientWithVisitAsync(patient, employeeId, visitDate);
        }

        public async Task AddVisitAsync(int patientId, int employeeId, DateTime visitDate)
        {
            await _patientRepository.AddVisitAsync(patientId, employeeId, visitDate);
        }
    }
}
