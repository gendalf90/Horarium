using System;
using Horarium.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Horarium.AspNetCore;
using Microsoft.Extensions.Configuration;
using Horarium.InMemory;
using Horarium.Mongo;
using Horarium.PostgreSql;
using System.Collections.Generic;
using Horarium.EF;
using Microsoft.EntityFrameworkCore;

namespace Horarium.IntegrationTest
{
    public class IntegrationTestBase : IDisposable
    {
        protected const string IntegrationTestCollection = "IntegrationTestCollection";

        private List<IServiceProvider> _providers = new();

        public IHorarium Initialize(Action<IServiceCollection> configuration)
        {
            var services = new ServiceCollection().AddLogging(builder =>
            {
                builder.AddDebug();
            });
            
            configuration(services);

            var settings = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .AddEnvironmentVariables()
                .Build();

            var dataBase = settings["DataBase"];

            var provider = dataBase switch
            {
                "InMemory" => services.AddHorariumServer(new InMemoryRepository()).BuildServiceProvider(),
                "MongoDB" => services.AddHorariumServer(MongoRepositoryFactory.Create(settings["MongoConnection"])).BuildServiceProvider(),
                "PostgreSql" => services
                    .AddHorariumServer(PostgreRepositoryFactory.Create(settings["PostgreConnection"]))
                    .BuildServiceProvider()
                    .CreateHorariumDatabase(builder =>
                    {
                        builder.UseNpgsql(settings["PostgreConnection"]);
                    }),
                _ => throw new ArgumentOutOfRangeException(nameof(dataBase), dataBase, null)
            };

            var server = provider.GetRequiredService<IHostedService>();

            server.StartAsync(default).Wait();

            _providers.Add(provider);

            return provider.GetRequiredService<IHorarium>();
        }

        public void Dispose()
        {
            foreach (var provider in _providers)
            {
                provider.GetService<IHostedService>()?.StopAsync(default).Wait();
            }
        }
    }
}
