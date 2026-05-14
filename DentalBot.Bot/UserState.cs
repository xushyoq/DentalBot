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
        WaitingDoctor,
        WaitingPatientSearch,
        WaitingPatientSelection
    }

    public class UserState
    {
        public UserStep Step { get; set; } = UserStep.None;
        public Patient? TempPatient { get; set; }
        public List<Patient> SearchResults { get; set; } = new();
    }
}
