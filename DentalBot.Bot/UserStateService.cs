using System.Collections.Concurrent;

namespace DentalBot.Bot
{
    public class UserStateService
    {
        private readonly ConcurrentDictionary<long, UserState> _states = new();

        public UserState GetState(long telegramId)
        {
            return _states.GetOrAdd(telegramId, _ => new UserState());
        }

        public void SetState(long telegramId, UserState state)
        {
            _states[telegramId] = state;
        }

        public void ClearState(long telegramId)
        {
            _states[telegramId] = new UserState();
        }
    }
}
