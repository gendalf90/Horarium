using System.Threading.Tasks;
using Horarium.Interfaces;

namespace Horarium.IntegrationTest.Jobs.Fallback
{
    public class FallbackNextJob : IJob<int>
    {
        private readonly IDependency _dependency;

        public FallbackNextJob(IDependency dependency)
        {
            _dependency = dependency;
        }
        
        public async Task Execute(int param)
        {
            await _dependency.Call(param.ToString());
        }
    }
}