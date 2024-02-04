using System;
using Horarium.Repository;
using Npgsql;

namespace Horarium.PostgreSql
{
    public static class PostgreRepositoryFactory
    {
        public static IJobRepository Create(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString), "Connection string is empty");
            }

            var builder = new NpgsqlDataSourceBuilder(connectionString);

            return new PostgreRepository(builder.Build());
        }
    }
}