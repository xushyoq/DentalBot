using DentalBot.Domain.Entities;
namespace DentalBot.Application.Interfaces
{
    public interface IEmployeeRepository
    {
        public Task<Employee?> GetEmployeeByIdAsync(long telegramId);
        public Task<Employee?> CreateFirstEmployeeAsync(Employee employee);
        public Task<IEnumerable<Employee>> GetAllAsync();
    }
}
