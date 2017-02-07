using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using SFA.DAS.EmployerUsers.Api.Types;

namespace SFA.DAS.EmployerUsers.Api.AcceptanceTests.UserTests
{
    [TestFixture]
    public class WhenIGetAUser : ApiTestBase
    {
        [Test]
        public async Task ThenTheUserIsReturned()
        {
            var expectedUser = Users[2];
            var response = await Client.GetResource<UserViewModel>($"api/users/{expectedUser.Id}");

            response.ShouldBeEquivalentTo(expectedUser);
        }
    }
}
