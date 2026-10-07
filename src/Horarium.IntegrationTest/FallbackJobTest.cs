using System;
using System.Threading.Tasks;
using Horarium.IntegrationTest.Jobs;
using Horarium.IntegrationTest.Jobs.Fallback;
using Horarium.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Horarium.IntegrationTest
{
    [Collection(IntegrationTestCollection)]
    public class FallbackJobTest : IntegrationTestBase
    {
        private readonly IHorarium _horarium;
        private readonly Mock<IDependency> _dependency = new();
        private readonly TaskCompletionSource _completion = new();
        
        public FallbackJobTest()
        {
            _dependency
                .Setup(x => x.Call("1"))
                .Returns(Task.FromException(new Exception()));

            _dependency
                .Setup(x => x.Call("2"))
                .Returns(Task.CompletedTask);

            _dependency
                .Setup(x => x.Call("3"))
                .Callback(() => _completion.SetResult())
                .Returns(Task.CompletedTask);
            
            _horarium = Initialize(services =>
            {
                services.AddSingleton(_dependency.Object);
                services.AddTransient<FallbackMainJob>();
                services.AddTransient<FallbackJob>();
                services.AddTransient<FallbackNextJob>();
            });
        }
        
        [Fact]
        public async Task FallbackJobAdded_FallbackJobExecuted()
        {
            var mainJobRepeatCount = 2;

            await _horarium.Schedule<FallbackMainJob, int>(1, conf => 
                                                              conf.MaxRepeatCount(mainJobRepeatCount)
                                                                  .AddRepeatStrategy<FallbackRepeatStrategy>()
                                                                  .AddFallbackConfiguration(configure =>
                                                                      configure
                                                                          .ScheduleFallbackJob<FallbackJob, int>(
                                                                              2,
                                                                              builder =>
                                                                              {
                                                                                  builder
                                                                                      .Next<FallbackNextJob,
                                                                                          int>(3);
                                                                              })));

            await _completion.Task.WaitAsync(TimeSpan.FromSeconds(10));

            await Task.Delay(3000);

            _dependency.Verify(x => x.Call("1"), Times.Exactly(mainJobRepeatCount));
            _dependency.Verify(x => x.Call("2"), Times.Once());
            _dependency.Verify(x => x.Call("3"), Times.Once());
        }
        
        [Fact]
        public async Task FallbackJobGoNextStrategy_NextJobExecuted()
        {
            var mainJobRepeatCount = 2;

            await _horarium.Schedule<FallbackMainJob, int>(1, conf => 
                                                              conf.MaxRepeatCount(mainJobRepeatCount)
                                                                  .AddRepeatStrategy<FallbackRepeatStrategy>()
                                                                  .AddFallbackConfiguration(
                                                                      configure => configure.GoToNextJob())
                                                                  .Next<FallbackNextJob, int>(3)
            );
            
            await _completion.Task.WaitAsync(TimeSpan.FromSeconds(10));

            await Task.Delay(3000);

            _dependency.Verify(x => x.Call("1"), Times.Exactly(mainJobRepeatCount));
            _dependency.Verify(x => x.Call("3"), Times.Once());
        }
    }
}