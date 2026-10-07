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
    public class TestParallelsWorkTwoManagers : IntegrationTestBase
    {
        private readonly IHorarium _horariumFirst;
        private readonly IHorarium _horariumSecond;
        private readonly Mock<IDependency> _dependency = new();
        private readonly ConcurrentBag<string> _values = new();
        
        public TestParallelsWorkTwoManagers()
        {
            _dependency
                .Setup(x => x.Call(It.IsAny<string>()))
                .Callback<string>(value => _values.Add(value))
                .Returns(Task.CompletedTask);
            
            _horariumFirst = Initialize(services =>
            {
                services.AddSingleton(_dependency.Object);
                services.AddTransient<TestJob>();
            });

            _horariumSecond = Initialize(services =>
            {
                services.AddSingleton(_dependency.Object);
                services.AddTransient<TestJob>();
            });
        }
        
        [Fact]
        public async Task TestParallels()
        {
            for (var i = 0; i < 1000; i++)
            {
                await _horariumFirst.Create<TestJob, int>(i).Schedule();

                await Task.Delay(10);
            }

            await Task.Delay(10000);

            Assert.NotEmpty(_values);

            Assert.False(_values.GroupBy(x => x).Any(g => g.Count() > 1), "Same job was executed multiple times");
        }
    }
}