using DentalBot.Application.Interfaces;
using DentalBot.Domain.Entities;
using DentalBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DentalBot.Infrastructure.Repositories
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly AppDbContext _context;

        public EmployeeRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Employee?> GetEmployeeByIdAsync(long telegramId)
        {
            return await _context.Employees
                .FirstOrDefaultAsync(e => e.TelegramId == telegramId);
        }

        public async Task<Employee?> CreateFirstEmployeeAsync(Employee employee)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            await _context.Database.ExecuteSqlRawAsync("LOCK TABLE \"Employees\" IN EXCLUSIVE MODE");

            if (await _context.Employees.AnyAsync())
            {
                await transaction.RollbackAsync();
                return null;
            }

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return employee;
        }

        public async Task<IEnumerable<Employee>> GetAllAsync()
        {
            return await _context.Employees.ToListAsync();
        }
    }
}
