using System.Collections.Generic;
using Microsoft.Azure;
using NUnit.Framework;
using SFA.DAS.Configuration;
using SFA.DAS.Configuration.AzureTableStorage;
using SFA.DAS.EmployerUsers.Api.AcceptanceTests.Builders;
using SFA.DAS.EmployerUsers.Api.Client;
using SFA.DAS.EmployerUsers.Api.Types;

namespace SFA.DAS.EmployerUsers.Api.AcceptanceTests
{
    public abstract class ApiTestBase
    {
        private const string ServiceName = "SFA.DAS.EmployerUsers.Api.AcceptanceTests";
        protected EmployerUsersApiConfiguration ClientConfiguration;
        protected EmployerUsersApiClient Client;
        protected List<UserViewModel> Users;

        [SetUp]
        public void Arrange()
        {
            ClientConfiguration = GetClientConfiguration();
            Client = new EmployerUsersApiClient(ClientConfiguration);

            Users = CreateUsers();
            SetupDatabase();
        }

        protected virtual List<UserViewModel> CreateUsers()
        {
            return new List<UserViewModel>
            {
                new UserViewModelBuilder().WithId("123").WithName("Test", "Test").Build(),
                new UserViewModelBuilder().WithId("456").WithName("Joe", "Bloggs").Build(),
                new UserViewModelBuilder().WithId("789").WithName("Joanne", "Bloggs").Build(),
                new UserViewModelBuilder().WithId("987").WithName("Joe", "Test").Build(),
            };
        }

        protected virtual void SetupDatabase()
        {
            var repository = new ApiTestsRepository(CloudConfigurationManager.GetSetting("UsersConnectionString"));
            repository.DeleteUsers().Wait();

            Users.ForEach(async x => await repository.CreateUser(x));
        }

        private EmployerUsersApiConfiguration GetClientConfiguration()
        {
            var configurationRepository = new AzureTableStorageConfigurationRepository(CloudConfigurationManager.GetSetting("ConfigurationStorageConnectionString"));
            var environment = CloudConfigurationManager.GetSetting("EnvironmentName");
            var configurationService = new ConfigurationService(configurationRepository, new ConfigurationOptions(ServiceName, environment, "1.0"));
            return configurationService.Get<EmployerUsersApiConfiguration>();
        }
    }
}
