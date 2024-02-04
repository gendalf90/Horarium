using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Horarium.Fallbacks;
using Horarium.Repository;
using Npgsql;
using NpgsqlTypes;

namespace Horarium.PostgreSql
{
    internal class PostgreRepository : IJobRepository
    {
        private readonly NpgsqlDataSource _dataSource;

        public PostgreRepository(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        async Task IJobRepository.AddJob(JobDb job)
        {
            var builder = new StringBuilder();
            var nextJobs = new List<JobDb>();

            AddInsertPlaceholder(builder);

            var shift = AddParametersPlaceholder(builder);

            job.Unwind(nextJobs);

            for (int i = 0; i < nextJobs.Count; i++)
            {
                shift = AddParametersPlaceholder(builder, true, shift);
            }

            builder.AppendLine(@"ON CONFLICT (""JobId"") DO UPDATE SET 
                ""ParentJobId"" = CASE WHEN public.""horarium.jobs"".""JobId"" = $1 THEN null ELSE $1 END,
                ""StartAt"" = EXCLUDED.""StartAt""");

            await using var conn = await _dataSource.OpenConnectionAsync();

            await using var cmd = new NpgsqlCommand(builder.ToString(), conn);
            
            AddInsertParameters(cmd.Parameters, job, null);

            foreach (var nextJob in nextJobs)
            {
                AddInsertParameters(cmd.Parameters, nextJob, job.JobId);
            }

            await cmd.ExecuteNonQueryAsync();
        }

        private static int AddParametersPlaceholder(StringBuilder builder, bool isAppend = false, int shift = 1)
        {
            const int ParametersCount = 20;

            var currentShift = shift;

            if (isAppend)
            {
                builder.Append(", ");
            }
            
            builder.Append($"(${currentShift++}");

            for (int i = 0; i < ParametersCount - 1; i++)
            {
                builder.Append($", ${currentShift++}");
            }

            builder.AppendLine(")");

            return currentShift;
        }

        private static void AddInsertPlaceholder(StringBuilder builder)
        {
            builder.AppendLine(@"
                INSERT INTO public.""horarium.jobs"" 
                (
                    ""JobId"", 
                    ""JobKey"", 
                    ""JobType"",
                    ""JobParamType"",
                    ""JobParam"",
                    ""Status"", 
                    ""CountStarted"",
                    ""ExecutedMachine"",
                    ""StartedExecuting"", 
                    ""StartAt"", 
                    ""NextJobId"",
                    ""Error"",
                    ""Cron"",
                    ""Delay"",
                    ""ObsoleteInterval"",
                    ""RepeatStrategy"",
                    ""MaxRepeatCount"",
                    ""FallbackJobId"",
                    ""FallbackStrategyType"",
                    ""ParentJobId""
                )
                VALUES");
        }

        private static void AddInsertParameters(NpgsqlParameterCollection parameters, JobDb job, string parentId)
        {
            parameters.AddWithValue(NpgsqlDbType.Text, ValueOrNull(job.JobId));
            parameters.AddWithValue(NpgsqlDbType.Text, ValueOrNull(job.JobKey));
            parameters.AddWithValue(NpgsqlDbType.Text, ValueOrNull(job.JobType));
            parameters.AddWithValue(NpgsqlDbType.Text, ValueOrNull(job.JobParamType));
            parameters.AddWithValue(NpgsqlDbType.Text, ValueOrNull(job.JobParam));
            parameters.AddWithValue(NpgsqlDbType.Integer, (int)job.Status);
            parameters.AddWithValue(NpgsqlDbType.Integer, job.CountStarted);
            parameters.AddWithValue(NpgsqlDbType.Text, ValueOrNull(job.ExecutedMachine)); 
            parameters.AddWithValue(NpgsqlDbType.TimestampTz, job.StartedExecuting); 
            parameters.AddWithValue(NpgsqlDbType.TimestampTz, job.StartAt);
            parameters.AddWithValue(NpgsqlDbType.Text, ValueOrNull(job.NextJob?.JobId));
            parameters.AddWithValue(NpgsqlDbType.Text, ValueOrNull(job.Error));
            parameters.AddWithValue(NpgsqlDbType.Text, ValueOrNull(job.Cron));
            parameters.AddWithValue(NpgsqlDbType.Interval, ValueOrNull(job.Delay));
            parameters.AddWithValue(NpgsqlDbType.Interval, job.ObsoleteInterval);
            parameters.AddWithValue(NpgsqlDbType.Text, ValueOrNull(job.RepeatStrategy));
            parameters.AddWithValue(NpgsqlDbType.Integer, job.MaxRepeatCount);
            parameters.AddWithValue(NpgsqlDbType.Text, ValueOrNull(job.FallbackJob?.JobId));
            parameters.AddWithValue(NpgsqlDbType.Integer, ValueOrNull((int?)job.FallbackStrategyType));
            parameters.AddWithValue(NpgsqlDbType.Text, ValueOrNull(parentId));
        }

        private static object ValueOrNull(object value)
        {
            if (value == null)
            {
                return DBNull.Value;
            }

            return value;
        }

        async Task IJobRepository.AddRecurrentJob(JobDb job)
        {
            var builder = new StringBuilder();

            AddInsertPlaceholder(builder);
            AddParametersPlaceholder(builder);

            builder.AppendLine(@"ON CONFLICT (""JobKey"") DO UPDATE SET ""Cron"" = $13, ""StartAt"" = $10;");

            await using var conn = await _dataSource.OpenConnectionAsync();

            await using var delete = new NpgsqlCommand(@"DELETE FROM public.""horarium.jobs"" WHERE ""JobKey"" = $1 AND ""Status"" = 2", conn);

            delete.Parameters.AddWithValue(NpgsqlDbType.Text, job.JobKey);

            await delete.ExecuteNonQueryAsync();

            await using var insert = new NpgsqlCommand(builder.ToString(), conn);

            AddInsertParameters(insert.Parameters, job, null);

            await insert.ExecuteNonQueryAsync();
        }

        Task IJobRepository.AddRecurrentJobSettings(RecurrentJobSettings settings)
        {
            return Task.CompletedTask;
        }

        async Task IJobRepository.FailedJob(string jobId, Exception error)
        {
            var errorMessage = GetErrorMessage(error);
            
            await using var conn = await _dataSource.OpenConnectionAsync();

            await using var cmd = new NpgsqlCommand(@"
                UPDATE public.""horarium.jobs"" 
                SET ""Status"" = 2, ""Error"" = $2
                WHERE ""JobId"" = $1
            ", conn);

            cmd.Parameters.AddWithValue(NpgsqlDbType.Text, ValueOrNull(jobId));
            cmd.Parameters.AddWithValue(NpgsqlDbType.Text, ValueOrNull(errorMessage));

            await cmd.ExecuteNonQueryAsync();
        }

        async Task<Dictionary<JobStatus, int>> IJobRepository.GetJobStatistic()
        {
            var result = new Dictionary<JobStatus, int>();
            
            await using var conn = await _dataSource.OpenConnectionAsync();

            await using var cmd = new NpgsqlCommand(@"
                SELECT ""Status"" as status, COUNT(*) AS sum
                FROM public.""horarium.jobs"" 
                WHERE ""ParentJobId"" IS NULL
                GROUP BY ""Status""
            ", conn);

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result[(JobStatus)reader.GetInt32("status")] = reader.GetInt32("sum");
            }

            foreach (JobStatus jobStatus in Enum.GetValues(typeof(JobStatus)))
            {
                if (!result.ContainsKey(jobStatus))
                {
                    result.Add(jobStatus, 0);
                }
            }

            return result;
        }

        async Task<JobDb> IJobRepository.GetReadyJob(string machineName, TimeSpan obsoleteTime)
        {
            var results = new List<(JobDb Job, string NextJobId, string FallbackJobId, string ParentJobId)>();
            
            await using var conn = await _dataSource.OpenConnectionAsync();

            await using var cmd = new NpgsqlCommand(@"
                WITH 
                    found AS (
                        SELECT ""JobId""
                        FROM public.""horarium.jobs""
                        WHERE (""ParentJobId"" IS NULL AND ""StartAt"" < NOW() AND ""Status"" IN (0, 4)) OR (""ParentJobId"" IS NULL AND ""StartedExecuting"" < NOW() - $1 AND ""Status"" = 1)
                        LIMIT 1
                        FOR UPDATE SKIP LOCKED
                    ),
                    updated AS (
                        UPDATE public.""horarium.jobs"" jobs
                        SET ""Status"" = 1, ""ExecutedMachine"" = $2, ""StartedExecuting"" = NOW(), ""CountStarted"" = jobs.""CountStarted"" + 1
                        FROM found
                        WHERE jobs.""JobId"" = found.""JobId""
                        RETURNING jobs.*
                    )
                SELECT jobs.* 
                FROM public.""horarium.jobs"" jobs 
                INNER JOIN updated ON jobs.""ParentJobId"" = updated.""JobId""
                UNION
                SELECT updated.*
                FROM updated
            ", conn);

            cmd.Parameters.AddWithValue(NpgsqlDbType.Interval, obsoleteTime);
            cmd.Parameters.AddWithValue(NpgsqlDbType.Text, ValueOrNull(machineName));

            await using var reader = await cmd.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                results.Add((
                    Job: new JobDb
                    {
                        JobId = GetValueOrNull<string>(reader, "JobId"),
                        JobKey = GetValueOrNull<string>(reader, "JobKey"),
                        JobType = GetValueOrNull<string>(reader, "JobType"),
                        JobParamType = GetValueOrNull<string>(reader, "JobParamType"),
                        JobParam = GetValueOrNull<string>(reader, "JobParam"),
                        Status = (JobStatus)reader.GetInt32("Status"),
                        CountStarted = reader.GetInt32("CountStarted"),
                        ExecutedMachine = GetValueOrNull<string>(reader, "ExecutedMachine"),
                        StartedExecuting = reader.GetDateTime("StartedExecuting"),
                        StartAt = reader.GetDateTime("StartAt"),
                        Error = GetValueOrNull<string>(reader, "Error"),
                        Cron = GetValueOrNull<string>(reader, "Cron"),
                        Delay = GetValueOrNullable<TimeSpan>(reader, "Delay"),
                        ObsoleteInterval = reader.GetFieldValue<TimeSpan>("ObsoleteInterval"),
                        RepeatStrategy = GetValueOrNull<string>(reader, "RepeatStrategy"),
                        MaxRepeatCount = reader.GetInt32("MaxRepeatCount"),
                        FallbackStrategyType = (FallbackStrategyTypeEnum?)GetValueOrNullable<int>(reader, "FallbackStrategyType")
                    },
                    NextJobId: GetValueOrNull<string>(reader, "NextJobId"),
                    FallbackJobId: GetValueOrNull<string>(reader, "FallbackJobId"),
                    ParentJobId: GetValueOrNull<string>(reader, "ParentJobId")
                ));
            }
            
            return Combine(results.FirstOrDefault(result => result.ParentJobId == null), results);
        }

        private static T GetValueOrNull<T>(NpgsqlDataReader reader, string name) where T : class
        {
            if (reader.IsDBNull(name))
            {
                return null;
            }

            return reader.GetFieldValue<T>(name);
        }

        private static T? GetValueOrNullable<T>(NpgsqlDataReader reader, string name) where T : struct
        {
            if (reader.IsDBNull(name))
            {
                return null;
            }

            return reader.GetFieldValue<T>(name);
        }

        private static JobDb Combine(
            (JobDb Job, string NextJobId, string FallbackJobId, string ParentJobId) parent, 
            List<(JobDb Job, string NextJobId, string FallbackJobId, string ParentJobId)> values)
        {
            if (parent == default)
            {
                return null;
            }
            
            parent.Job.NextJob = Combine(values.FirstOrDefault(value => value.Job.JobId == parent.NextJobId), values);
            parent.Job.FallbackJob = Combine(values.FirstOrDefault(value => value.Job.JobId == parent.FallbackJobId), values);

            return parent.Job;
        }

        async Task IJobRepository.RemoveJob(string jobId)
        {
            await using var conn = await _dataSource.OpenConnectionAsync();

            await using var cmd = new NpgsqlCommand(@"
                DELETE FROM public.""horarium.jobs"" WHERE ""JobId"" = $1
            ", conn);

            cmd.Parameters.AddWithValue(NpgsqlDbType.Text, ValueOrNull(jobId));

            await cmd.ExecuteNonQueryAsync();
        }

        async Task IJobRepository.RepeatJob(string jobId, DateTime startAt, Exception error)
        {
            var errorMessage = GetErrorMessage(error);
            
            await using var conn = await _dataSource.OpenConnectionAsync();

            await using var cmd = new NpgsqlCommand(@"
                UPDATE public.""horarium.jobs"" 
                SET ""Status"" = 4, ""StartAt"" = $2, ""Error"" = $3
                WHERE ""JobId"" = $1
            ", conn);

            cmd.Parameters.AddWithValue(NpgsqlDbType.Text, ValueOrNull(jobId));
            cmd.Parameters.AddWithValue(NpgsqlDbType.TimestampTz, startAt);
            cmd.Parameters.AddWithValue(NpgsqlDbType.Text, ValueOrNull(errorMessage));

            await cmd.ExecuteNonQueryAsync();
        }

        async Task IJobRepository.RescheduleRecurrentJob(string jobId, DateTime startAt, Exception error)
        {
            var errorMessage = GetErrorMessage(error);
            
            await using var conn = await _dataSource.OpenConnectionAsync();

            await using var cmd = new NpgsqlCommand(@"
                UPDATE public.""horarium.jobs"" 
                SET ""Status"" = 0, ""StartAt"" = $2, ""Error"" = $3
                WHERE ""JobId"" = $1
            ", conn);

            cmd.Parameters.AddWithValue(NpgsqlDbType.Text, ValueOrNull(jobId));
            cmd.Parameters.AddWithValue(NpgsqlDbType.TimestampTz, startAt);
            cmd.Parameters.AddWithValue(NpgsqlDbType.Text, ValueOrNull(errorMessage));

            await cmd.ExecuteNonQueryAsync();
        }

        private string GetErrorMessage(Exception error)
        {
            return error == null
                ? null
                : $"{error.Message} {error.StackTrace}";
        }
    }
}