using System;
using System.Collections.Generic;
using System.Text;

namespace DentalBot.Bot
{
    public class UserStateService
    {
        private readonly Dictionary<long, UserState> _states = new();

        public UserState GetState(long telegramId)
        {
            if (!_states.ContainsKey(telegramId))
                _states[telegramId] = new UserState();
            return _states[telegramId];
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

