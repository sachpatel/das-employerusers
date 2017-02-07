using System.Runtime.InteropServices;
using SFA.DAS.EmployerUsers.Api.Types;

namespace SFA.DAS.EmployerUsers.Api.AcceptanceTests.Builders
{
    public class UserViewModelBuilder
    {
        private string _email = "test@mail.com";
        private int _failedLoginAttempts = 2;
        private string _lastName = "Last Name";
        private string _firstName = "First Name";
        private string _id = "ABC123";
        private bool _isActive = true;
        private bool _isLocked = true;

        public UserViewModelBuilder WithId(string id)
        {
            _id = id;
            return this;
        }

        public UserViewModelBuilder WithName(string firstName, string lastName)
        {
            _firstName = firstName;
            _lastName = lastName;
            return this;
        }

        public UserViewModel Build()
        {
            return new UserViewModel
            {
                Email = _email,
                FailedLoginAttempts = _failedLoginAttempts,
                LastName = _lastName,
                FirstName = _firstName,
                Id = _id,
                IsActive = _isActive,
                IsLocked = _isLocked
            };
        }
    }
}
