using DentalBot.Application.Interfaces;
using DentalBot.Domain.Entities;

namespace DentalBot.Application.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IEmployeeRepository _employeeRepository;

        public EmployeeService(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public async Task<bool> AuthService(long telegramId)
        {
            var employee = await _employeeRepository.GetEmployeeByIdAsync(telegramId);
            return employee != null;
        }

        public async Task<(bool IsAuthenticated, bool WasCreated)> AuthOrRegisterFirstUserAsync(long telegramId, string firstName, string lastName)
        {
            var employee = await _employeeRepository.GetEmployeeByIdAsync(telegramId);
            if (employee != null)
            {
                return (true, false);
            }

            var createdEmployee = await _employeeRepository.CreateFirstEmployeeAsync(new Employee
            {
                TelegramId = telegramId,
                FirstName = string.IsNullOrWhiteSpace(firstName) ? telegramId.ToString() : firstName.Trim(),
                LastName = lastName.Trim()
            });

            return (createdEmployee != null, createdEmployee != null);
        }

        public async Task<IEnumerable<Employee>> GetAllAsync()
        {
            return await _employeeRepository.GetAllAsync();
        }
    }
}
