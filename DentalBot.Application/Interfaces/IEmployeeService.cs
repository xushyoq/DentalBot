using DentalBot.Domain.Entities;

namespace DentalBot.Application.Interfaces
{
    public interface IEmployeeService
    {
        public Task<bool> AuthService(long telegramId);
        public Task<(bool IsAuthenticated, bool WasCreated)> AuthOrRegisterFirstUserAsync(long telegramId, string firstName, string lastName);
        public Task<IEnumerable<Employee>> GetAllAsync();
    }
}
