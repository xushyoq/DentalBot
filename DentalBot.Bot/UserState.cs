using DentalBot.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace DentalBot.Bot
{
    public enum UserStep
    {
        None,
        WaitingFirstName,
        WaitingLastName,
        WaitingBirthYear,
        WaitingPhone,
        WaitingAddress,
        WaitingWorkplace,
        WaitingDoctor
    }

    public class UserState
    {
        public UserStep Step { get; set; } = UserStep.None;
        public Patient? TempPatient { get; set; }
    }
}
