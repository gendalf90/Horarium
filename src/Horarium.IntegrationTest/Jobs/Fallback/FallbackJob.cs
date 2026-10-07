using System.Threading.Tasks;
using Horarium.Interfaces;

namespace Horarium.IntegrationTest.Jobs.Fallback
{
    public class FallbackJob : IJob<int>
    {
        private readonly IDependency _dependency;

        public FallbackJob(IDependency dependency)
        {
            _dependency = dependency;
        }
        
        public async Task Execute(int param)
        {
            await _dependency.Call(param.ToString());
        }
    }
}