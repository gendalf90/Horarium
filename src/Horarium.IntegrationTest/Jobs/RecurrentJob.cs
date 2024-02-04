using System.Threading.Tasks;
using Horarium.Interfaces;

namespace Horarium.IntegrationTest.Jobs
{
    public class RecurrentJob : IJobRecurrent
    {
        private readonly IDependency _dependency;

        public RecurrentJob(IDependency dependency)
        {
            _dependency = dependency;
        }

        public async Task Execute()
        {
            await _dependency.Call(TestParam);
        }

        public static string TestParam => nameof(RecurrentJob);
    }
}