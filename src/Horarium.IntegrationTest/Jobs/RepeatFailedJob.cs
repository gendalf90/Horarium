using System;
using System.Threading.Tasks;
using Horarium.Interfaces;

namespace Horarium.IntegrationTest.Jobs
{
    public class RepeatFailedJob : IJob<string>, IAllRepeatesIsFailed
    {
        private readonly IDependency _dependency;

        public RepeatFailedJob(IDependency dependency)
        {
            _dependency = dependency;
        }

        public async Task Execute(string param)
        {
            await _dependency.Call(param);
        }

        public async Task FailedEvent(object param, Exception ex)
        {
            await _dependency.Call(FailParam);
        }

        public static string FailParam => "failed";
    }
}