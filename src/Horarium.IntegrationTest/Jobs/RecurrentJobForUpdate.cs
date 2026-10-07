using System;
using System.Threading;
using System.Threading.Tasks;
using Horarium.Interfaces;

namespace Horarium.IntegrationTest.Jobs
{
    public class RecurrentJobForUpdate : IJobRecurrent
    {
        private readonly IDependency _dependency;

        public RecurrentJobForUpdate(IDependency dependency)
        {
            _dependency = dependency;
        }

        public async Task Execute()
        {
            await _dependency.Call(TestParam);

            await Task.Delay(TimeSpan.FromSeconds(25));
        }

        public static string TestParam => nameof(RecurrentJobForUpdate);
    }
}
