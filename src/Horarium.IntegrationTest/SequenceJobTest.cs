using System;
using System.Threading.Tasks;
using Horarium.IntegrationTest.Jobs;
using Horarium.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Horarium.IntegrationTest
{
    [Collection(IntegrationTestCollection)]
    public class SequenceJobTest : IntegrationTestBase
    {
        private readonly IHorarium _horarium;
        private readonly Mock<IDependency> _dependency = new();
        private readonly TaskCompletionSource _completion = new();

        public SequenceJobTest()
        {
            _dependency
                .Setup(x => x.Call("2"))
                .Callback(() => _completion.SetResult())
                .Returns(Task.CompletedTask);
            
            _horarium = Initialize(services =>
            {
                services.AddSingleton(_dependency.Object);
                services.AddTransient<SequenceJob>();
            });
        }
        
        [Fact]
        public async Task SequenceJobsAdded_ExecutedSequence()
        {
            await _horarium
                .Create<SequenceJob, int>(0)
                .Next<SequenceJob, int>(1)
                .Next<SequenceJob, int>(2)
                .Schedule();
            
            await _completion.Task.WaitAsync(TimeSpan.FromSeconds(10));

            await Task.Delay(3000);

            _dependency.Verify(x => x.Call("0"), Times.Once());
            _dependency.Verify(x => x.Call("1"), Times.Once());
            _dependency.Verify(x => x.Call("2"), Times.Once());
        }
    }
}