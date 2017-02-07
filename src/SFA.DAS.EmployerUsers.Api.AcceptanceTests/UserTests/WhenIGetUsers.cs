using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace SFA.DAS.EmployerUsers.Api.AcceptanceTests.UserTests
{
    [TestFixture]
    public class WhenIGetUsers : ApiTestBase
    {
        [Test]
        public async Task ThenUsersAreReturned()
        {
            var response = await Client.GetPageOfEmployerUsers();

            response.Data.Should().HaveCount(4);
        }
    }
}
