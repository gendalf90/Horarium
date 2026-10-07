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
    public class OneTimeJobTest : IntegrationTestBase
    {
        private readonly IHorarium _horarium;
        private readonly Mock<IDependency> _dependency = new();
        private readonly TaskCompletionSource _completion = new();
        
        public OneTimeJobTest()
        {
            _dependency
                .Setup(x => x.Call(It.IsAny<string>()))
                .Callback(() => _completion.SetResult())
                .Returns(Task.CompletedTask);
            
            _horarium = Initialize(services =>
            {
                services.AddSingleton(_dependency.Object);
                services.AddTransient<OneTimeJob>();
            });
        }
        
        [Fact]
        public async Task OneTimeJob_RunAfterAdded()
        {
            await _horarium.Create<OneTimeJob, int>(5).Schedule();
            
            await _completion.Task.WaitAsync(TimeSpan.FromSeconds(10));

            await Task.Delay(3000);

            _dependency.Verify(x => x.Call("5"), Times.Once());
        }
    }
}