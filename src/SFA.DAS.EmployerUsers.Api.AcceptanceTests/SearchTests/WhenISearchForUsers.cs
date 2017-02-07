using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace SFA.DAS.EmployerUsers.Api.AcceptanceTests.SearchTests
{
    [TestFixture]
    public class WhenISearchForUsers : ApiTestBase
    {
        [Test]
        public async Task ThenUsersAreReturned()
        {
            var response = await Client.SearchEmployerUsers("blogg");

            response.Data.Should().HaveCount(2);
        }
    }
}
