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

        public async Task<IEnumerable<PatientVisit>> GetVisitsByDateAsync(DateTime date)
        {
            var timeZone = GetTashkentTimeZone();
            var localStart = DateTime.SpecifyKind(date.Date, DateTimeKind.Unspecified);
            var start = TimeZoneInfo.ConvertTimeToUtc(localStart, timeZone);
            var end = start.AddDays(1);

            return await _context.PatientVisits
                .Include(visit => visit.Patient)
                .Include(visit => visit.Employee)
                .Where(visit => visit.VisitDate >= start && visit.VisitDate < end)
                .OrderBy(visit => visit.VisitDate)
                .ToListAsync();
        }

        public async Task<Patient?> GetByIdAsync(int id)
        {
            return await _context.Patients.FindAsync(id);
        }

        public async Task<IEnumerable<Patient>> SearchAsync(string query, int maxResults)
        {
            var pattern = $"%{query.Trim()}%";

            return await _context.Patients
                .Where(patient =>
                    EF.Functions.ILike(patient.FirstName, pattern) ||
                    EF.Functions.ILike(patient.LastName, pattern) ||
                    EF.Functions.ILike(patient.Phone, pattern))
                .OrderByDescending(patient => patient.Id)
                .Take(maxResults)
                .ToListAsync();
        }

        public async Task<int> GetVisitCountAsync(int patientId)
        {
            return await _context.PatientVisits
                .CountAsync(visit => visit.PatientId == patientId);
        }

        public async Task<PatientVisit?> GetLastVisitAsync(int patientId)
        {
            return await _context.PatientVisits
                .Include(visit => visit.Employee)
                .Where(visit => visit.PatientId == patientId)
                .OrderByDescending(visit => visit.VisitDate)
                .FirstOrDefaultAsync();
        }

        public async Task<Patient> UpdateAsync(Patient patient)
        {
            _context.Patients.Update(patient);
            await _context.SaveChangesAsync();
            return patient;
        }

        public async Task AddPatientWithVisitAsync(Patient patient, int employeeId, DateTime visitDate)
        {
            patient.Visits.Add(new PatientVisit
            {
                EmployeeId = employeeId,
                VisitDate = ToUtc(visitDate)
            });

            await _context.Patients.AddAsync(patient);
            await _context.SaveChangesAsync();
        }

        public async Task AddVisitAsync(int patientId, int employeeId, DateTime visitDate)
        {
            var patientVisit = new PatientVisit
            {
                PatientId = patientId,
                EmployeeId = employeeId,
                VisitDate = ToUtc(visitDate)
            };

            await _context.PatientVisits.AddAsync(patientVisit);
            await _context.SaveChangesAsync();
        }

        private static DateTime ToUtc(DateTime dateTime)
        {
            return dateTime.Kind switch
            {
                DateTimeKind.Utc => dateTime,
                DateTimeKind.Local => dateTime.ToUniversalTime(),
                _ => TimeZoneInfo.ConvertTimeToUtc(dateTime, GetTashkentTimeZone())
            };
        }

        private static TimeZoneInfo GetTashkentTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Asia/Tashkent");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("West Asia Standard Time");
            }
        }
    }
}
