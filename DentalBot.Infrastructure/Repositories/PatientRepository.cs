using DentalBot.Application.Interfaces;
using DentalBot.Domain.Entities;
using DentalBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace DentalBot.Infrastructure.Repositories
{
    public class PatientRepository : IPatientRepository
    {
        private readonly AppDbContext _context;

        public PatientRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<Patient> AddAsync(Patient patient)
        {
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            return patient;
        }

        public async Task<Patient> DeleteAsync(Patient patient)
        {
            _context.Patients.Remove(patient);
            await _context.SaveChangesAsync();
                
            return patient;
        }

        public async Task<IEnumerable<Patient>> GetAllAsync()
        {
            return await _context.Patients.ToListAsync();
        }

        public async Task<IEnumerable<Patient>> GetByVisitDateAsync(DateTime date)
        {
            var start = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
            var end = start.AddDays(1);

            return await _context.Patients
                .Where(patient => patient.VisitDate >= start && patient.VisitDate < end)
                .OrderBy(patient => patient.Id)
                .ToListAsync();
        }

        public async Task<Patient?> GetByIdAsync(int id)
        {
            return await _context.Patients.FindAsync(id);
        }

        public async Task<Patient> UpdateAsync(Patient patient)
        {
            _context.Patients.Update(patient);
            await _context.SaveChangesAsync();
            return patient;
        }

        public async Task AddPatientWithEmployeeAsync(Patient patient, int employeeId)
        {
            await _context.Patients.AddAsync(patient);
            await _context.SaveChangesAsync();

            var patientEmployee = new PatientEmployee
            {
                PatientId = patient.Id,
                EmployeeId = employeeId
            };

            await _context.PatientEmployees.AddAsync(patientEmployee);
            await _context.SaveChangesAsync();
        }
    }
}
