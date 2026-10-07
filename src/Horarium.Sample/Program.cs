using System;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Horarium.Mongo;
using Horarium.PostgreSql;
using Horarium.Repository;
using Horarium.InMemory;
using Microsoft.EntityFrameworkCore;
using Horarium.EF;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Horarium.Sample
{
    static class Program
    {
        class HorariumContext : DbContext
        {
            private readonly string _connection;

            public HorariumContext(string connection)
            {
                _connection = connection;
            }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                optionsBuilder.UseNpgsql(_connection);
            }
            
            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.UseHorarium();
            }
        }
        
        static async Task Main()
        {
            var settings = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var dataBase = settings["DataBase"];
            var mode = settings["Mode"];

            IJobRepository repository = dataBase switch
            {
                "InMemory" => new InMemoryRepository(),
                "MongoDB" => MongoRepositoryFactory.Create(settings["MongoConnection"]),
                "PostgreSql" => InitPostgreRepository(settings["PostgreConnection"]),
                _ => throw new ArgumentOutOfRangeException(nameof(dataBase), dataBase, "Repository type is unknown")
            };

            if (mode == "Sample")
            {
                await StartScheduler(repository);
            }
            else if (mode == "Load")
            {
                await StartLoad(repository);
            }
            else throw new ArgumentOutOfRangeException(nameof(mode), mode, "Mode is unknown");
            
            Console.WriteLine("Start");
            Console.Read();
        }

        static IJobRepository InitPostgreRepository(string connection)
        {
            using var context = new HorariumContext(connection);

            context.Database.EnsureCreated();

            return PostgreRepositoryFactory.Create(connection);
        }

        static async Task StartScheduler(IJobRepository repository)
        {
            var horarium = new HorariumServer(repository);

            await horarium.CreateRecurrent<TestRecurrentJob>(Cron.SecondInterval(10)).Schedule();

            var firstJobDelay = TimeSpan.FromSeconds(20);

            var secondJobDelay = TimeSpan.FromSeconds(15);

            await horarium.Schedule<TestJob, int>(1, conf => conf // 1-st job
                .WithDelay(firstJobDelay)
                .Next<TestJob, int>(2) // 2-nd job
                .WithDelay(secondJobDelay)
                .Next<TestJob, int>(3) // 3-rd job (global obsolete from settings and no delay will be applied)
                .Next<FailedTestJob, int>(4) // 4-th job failed with exception
                .AddRepeatStrategy<CustomRepeatStrategy>()
                .MaxRepeatCount(3)
                .AddFallbackConfiguration(
                    x => x.GoToNextJob()) // execution continues after all attempts
                .Next<FailedTestJob, int>(5) // 5-th job job failed with exception
                .MaxRepeatCount(1)
                .AddFallbackConfiguration(
                    x => x.ScheduleFallbackJob<FallbackTestJob, int>(6, builder =>
                    {
                        builder.Next<TestJob, int>(7);
                    })) // 6-th and 7-th jobs executes after all retries 
            );

            horarium.Start();

            await PrintStatistic(repository);
        }

        static async Task PrintStatistic(IJobRepository repository)
        {
            var statistic = await repository.GetJobStatistic();
            var result = statistic.Aggregate(
                new StringBuilder().AppendLine("--------"), 
                (builder, entry) => builder.AppendLine($"Status: {entry.Key}, Count: {entry.Value}"),
                builder => builder.AppendLine("--------").ToString());

            Console.WriteLine(result);
        }

        static async Task StartLoad(IJobRepository repository)
        {
            var horarium = new HorariumClient(repository);

            var scheduleTime = Stopwatch.StartNew();

            for (int i = 0; i < 100; i++)
            {
                await horarium.CreateRecurrent<TestRecurrentJob>(Cron.SecondInterval(Random.Shared.Next(5, 10))).WithKey($"Recurrent_{i}").Schedule();
            }

            for (int i = 0; i < 1000; i++)
            {
                await horarium.Schedule<TestJob, int>(i, conf => conf
                    .Next<TestJob, int>(i)
                    .Next<TestJob, int>(i));
            }

            scheduleTime.Stop();

            Console.WriteLine($"Scheduling lasted: {scheduleTime.Elapsed}");

            TestJob.StartTimeMeasure(3000);

            for (int i = 0; i < 5; i++)
            {
                new HorariumServer(repository).Start();
            }
        }
    }
}