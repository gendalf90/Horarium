using System.Threading.Tasks;
using Horarium.Interfaces;

namespace Horarium.IntegrationTest.Jobs
{
    public class TestJob : IJob<int>
    {
        private readonly IDependency _dependency;

        public TestJob(IDependency dependency)
        {
            _dependency = dependency;
        }

        public async Task Execute(int param)
        {
            await _dependency.Call(param.ToString());

            await Task.Delay(30);
        }
    }
}