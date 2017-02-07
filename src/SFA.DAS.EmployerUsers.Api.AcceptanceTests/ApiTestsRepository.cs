using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using SFA.DAS.EmployerUsers.Api.Types;

namespace SFA.DAS.EmployerUsers.Api.AcceptanceTests
{
    public class ApiTestsRepository
    {
        private string _connectionString;

        public ApiTestsRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task DeleteUsers()
        {
            await WithConnection(async c =>
            {
                return await c.ExecuteAsync(
                    sql: "DELETE FROM [dbo].[User]",
                    commandType: CommandType.Text);
            });
        }

        public async Task CreateUser(UserViewModel user)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@Id", user.Id, DbType.String);
            parameters.Add("@FirstName", user.FirstName, DbType.String);
            parameters.Add("@LastName", user.LastName, DbType.String);
            parameters.Add("@Email", user.Email, DbType.String);
            parameters.Add("@Password", "PASSWORD", DbType.String);
            parameters.Add("@Salt", "SALT", DbType.String);
            parameters.Add("@PasswordProfileId", "ProfileId", DbType.String);
            parameters.Add("@IsActive", user.IsActive, DbType.Boolean);
            parameters.Add("@FailedLoginAttempts", user.FailedLoginAttempts, DbType.Int32);
            parameters.Add("@IsLocked", user.IsLocked, DbType.Boolean);
            await WithConnection(async c =>
            {
                return await c.ExecuteAsync(
                    sql: "[dbo].[CreateUser]",
                    commandType: CommandType.StoredProcedure,
                    param: parameters);
            });
        }

        private async Task<T> WithConnection<T>(Func<IDbConnection, Task<T>> getData)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                return await getData(connection);
            }
        }
    }
}
