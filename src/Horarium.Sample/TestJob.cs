using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Horarium.Interfaces;

namespace Horarium.Sample
{
    public class TestJob : IJob<int>
    {
        private static Stopwatch Stopwatch;
        private static int RunCount;
        private static int StopCount;
        
        public async Task Execute(int param)
        {
            Console.WriteLine(param);

            await Task.Run(() => 
            { 
                if (Interlocked.Increment(ref RunCount) == StopCount)
                {
                    Stopwatch.Stop();
                    
                    Console.WriteLine($"Test job started count: {StopCount}, It takes: {Stopwatch.Elapsed}");
                }
            });
        }

        public static void StartTimeMeasure(int stopRunCount)
        {
            RunCount = 0;
            StopCount = stopRunCount;
            Stopwatch = Stopwatch.StartNew();
        }
    }
}