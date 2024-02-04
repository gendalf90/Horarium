using System.Threading.Tasks;
using Horarium.Interfaces;

namespace Horarium.IntegrationTest.Jobs
{
    public class OneTimeJob : IJob<int>
    {
        private readonly IDependency _dependency;

        public OneTimeJob(IDependency dependency)
        {
            _dependency = dependency;
        }

        public async Task Execute(int param)
        {
            await _dependency.Call(param.ToString());
        }
    }
}