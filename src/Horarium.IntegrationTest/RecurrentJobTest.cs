using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Horarium.IntegrationTest.Jobs;
using Horarium.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Horarium.IntegrationTest
{
    [Collection(IntegrationTestCollection)]
    public class RecurrentJobTest : IntegrationTestBase
    {
        private readonly IHorarium _horarium;
        private readonly Mock<IDependency> _dependency = new();
        private readonly TaskCompletionSource _completion = new();
        private readonly ConcurrentQueue<DateTime> _executingTime = new();
        private readonly ConcurrentQueue<DateTime> _executingTimeForUpdate = new();
        
        public RecurrentJobTest()
        {
            _dependency
                .Setup(x => x.Call(RecurrentJob.TestParam))
                .Callback(() => 
                {
                    if (_executingTime.Count < 10)
                    {
                        _executingTime.Enqueue(DateTime.Now);

                        if (_executingTime.Count == 10)
                        {
                            _completion.SetResult();
                        }
                    }
                })
                .Returns(Task.CompletedTask);

            _dependency
                .Setup(x => x.Call(RecurrentJobForUpdate.TestParam))
                .Callback(() => _executingTimeForUpdate.Enqueue(DateTime.Now))
                .Returns(Task.CompletedTask);
            
            _horarium = Initialize(services =>
            {
                services.AddSingleton(_dependency.Object);
                services.AddTransient<RecurrentJob>();
                services.AddTransient<RecurrentJobForUpdate>();
            });
        }
        
        [Fact]
        public async Task RecurrentJob_RunEverySeconds()
        {
            await _horarium.CreateRecurrent<RecurrentJob>(Cron.Secondly()).Schedule();

            await _completion.Task.WaitAsync(TimeSpan.FromSeconds(15));

            var executingTimes = _executingTime.ToArray();

            Assert.NotEmpty(executingTimes);

            var nextJobTime = executingTimes.First();

            foreach (var time in executingTimes)
            {
                Assert.Equal(nextJobTime, time, TimeSpan.FromMilliseconds(999));
                nextJobTime = time.AddSeconds(1);
            }
        }

        /// <summary>
        /// Тест проверяет, что при одновременной регистрации одного джоба разными шедулерами первый начнет выполняться, а второй нет,
        /// т.к. для рекуррентных джобов одновременно может выполняться только один экземпляр
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Scheduler_SecondInstanceStart_MustUpdateRecurrentJobCronParameters()
        {
            var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            while (!cancellation.IsCancellationRequested)
            {
                await _horarium.CreateRecurrent<RecurrentJobForUpdate>(Cron.SecondInterval(1)).Schedule();

                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }

            Assert.Single(_executingTimeForUpdate);
        }
    }
}