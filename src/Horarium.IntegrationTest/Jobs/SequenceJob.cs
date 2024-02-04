using System.Threading.Tasks;
using Horarium.Interfaces;

namespace Horarium.IntegrationTest.Jobs
{
    public class SequenceJob : IJob<int>
    {
        private readonly IDependency _dependency;

        public SequenceJob(IDependency dependency)
        {
            _dependency = dependency;
        }

        public async Task Execute(int param)
        {
            await _dependency.Call(param.ToString());
        }
    }
}