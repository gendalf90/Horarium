using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Horarium.Fallbacks;
using Microsoft.EntityFrameworkCore;

namespace Horarium.EF
{
    [Index(nameof(JobKey), IsUnique = true)]
    [Index(nameof(ParentJobId), nameof(StartAt), nameof(Status))]
    [Index(nameof(ParentJobId), nameof(StartedExecuting), nameof(Status))]
    internal class JobModel
    {
        [Key]
        [Column("JobId")]
        public string JobId { get; init; }

        [Column("JobKey")]
        public string JobKey { get; init; }

        [Column("JobType")]
        public string JobType { get; init; }

        [Column("JobParamType")]
        public string JobParamType { get; init; }

        [Column("JobParam")]
        public string JobParam { get; init; }

        [Column("Status")]
        public JobStatus Status { get; init; }

        [Column("CountStarted")]
        public int CountStarted { get; init; }

        [Column("ExecutedMachine")]
        public string ExecutedMachine { get; init; }

        [Column("StartedExecuting")]
        public DateTime StartedExecuting { get; init; }

        [Column("StartAt")]
        public DateTime StartAt { get; init; }

        [Column("NextJobId")]
        public string NextJobId { get; init; }

        [Column("Error")]
        public string Error { get; init; }

        [Column("Cron")]
        public string Cron { get; init; }

        [Column("Delay")]
        public TimeSpan? Delay { get; init; }

        [Column("ObsoleteInterval")]
        public TimeSpan ObsoleteInterval { get; init; }

        [Column("RepeatStrategy")]
        public string RepeatStrategy { get; init; }

        [Column("MaxRepeatCount")]
        public int MaxRepeatCount { get; init; }

        [Column("FallbackJobId")]
        public string FallbackJobId { get; init; }

        [Column("FallbackStrategyType")]
        public FallbackStrategyTypeEnum? FallbackStrategyType { get; init; }

        [Column("ParentJobId")]
        public string ParentJobId { get; init; }
    }
}
