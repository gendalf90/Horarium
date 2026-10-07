using System.Collections.Generic;
using Horarium.Repository;

namespace Horarium.PostgreSql
{
    internal static class Extensions
    {
        public static void Unwind(this JobDb job, List<JobDb> results)
        {
            job.NextJob?.Unwind(results);
            job.FallbackJob?.Unwind(results);

            if (job.NextJob != null)
            {
                results.Add(job.NextJob);
            }

            if (job.FallbackJob != null)
            {
                results.Add(job.FallbackJob);
            }
        }
    }
}