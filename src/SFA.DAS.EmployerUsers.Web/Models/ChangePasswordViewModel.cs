﻿namespace SFA.DAS.EmployerUsers.Web.Models
{
    public class ChangePasswordViewModel : ViewModelBase
    {
        public string UserId { get; set; }
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }
}