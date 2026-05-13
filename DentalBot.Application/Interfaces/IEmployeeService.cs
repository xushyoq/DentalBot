using DentalBot.Domain.Entities;

namespace DentalBot.Application.Interfaces
{
    public interface IEmployeeService
    {
        public Task<bool> AuthService(long telegramId);
        public Task<IEnumerable<Employee>> GetAllAsync();
    }
}
