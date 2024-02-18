using System;
using Horarium.Repository;
using Npgsql;

namespace Horarium.PostgreSql
{
    public static class PostgreRepositoryFactory
    {
        public static IJobRepository Create(string connectionString, Action<NpgsqlDataSourceBuilder> builder = null, string scheme = "public")
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString), "Connection string is empty");
            }

            if (string.IsNullOrWhiteSpace(scheme))
            {
                throw new ArgumentNullException(nameof(scheme), "Scheme is empty");
            }

            var source = new NpgsqlDataSourceBuilder(connectionString);

            builder?.Invoke(source);

            return new PostgreRepository(source.Build(), scheme);
        }
    }
}