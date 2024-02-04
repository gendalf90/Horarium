using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Horarium.IntegrationTest.Jobs;
using Horarium.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Horarium.IntegrationTest
{
    [Collection(IntegrationTestCollection)]
    public class RepeatFailedJobTest : IntegrationTestBase
    {
        private readonly IHorarium _horarium;
        private readonly Mock<IDependency> _dependency = new();
        private readonly TaskCompletionSource _completion = new();
        private readonly ConcurrentQueue<DateTime> _executingTime = new();
        
        public RepeatFailedJobTest()
        {
            _dependency
                .Setup(x => x.Call("test"))
                .Callback(() => _executingTime.Enqueue(DateTime.Now))
                .Returns(Task.FromException(new Exception()));

            _dependency
                .Setup(x => x.Call(RepeatFailedJob.FailParam))
                .Callback(() => _completion.SetResult())
                .Returns(Task.CompletedTask);
            
            _horarium = Initialize(services =>
            {
                services.AddSingleton(_dependency.Object);
                services.AddTransient<RepeatFailedJob>();
            });
        }
        
        [Fact]
        public async Task RepeatFailedJob_UseRepeatStrategy()
        {
            await _horarium
                .Create<RepeatFailedJob, string>("test")
                .AddRepeatStrategy<RepeatFailedJobTestStrategy>()
                .MaxRepeatCount(5)
                .Schedule();

            await _completion.Task.WaitAsync(TimeSpan.FromSeconds(10));

            await Task.Delay(3000);
            
            var executingTimes = _executingTime.ToArray();

            Assert.Equal(5, executingTimes.Length);

            var nextJobTime = executingTimes.First();

            foreach (var time in executingTimes)
            {
                Assert.Equal(nextJobTime, time, TimeSpan.FromMilliseconds(999));

                nextJobTime = time.AddSeconds(1);
            }
        }
    }
    
    public class RepeatFailedJobTestStrategy:  IFailedRepeatStrategy
    {
        public TimeSpan GetNextStartInterval(int countStarted)
        {
            return TimeSpan.FromSeconds(1);
        }
    }
}