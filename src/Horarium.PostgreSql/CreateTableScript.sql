CREATE TABLE IF NOT EXISTS public."horarium.jobs"
(
    "JobId" text NOT NULL,
    "JobKey" text,
    "JobType" text,
    "JobParamType" text,
    "JobParam" text,
    "Status" integer NOT NULL,
    "CountStarted" integer NOT NULL,
    "ExecutedMachine" text,
    "StartedExecuting" timestamp with time zone NOT NULL,
    "StartAt" timestamp with time zone NOT NULL,
    "NextJobId" text,
    "Error" text,
    "Cron" text,
    "Delay" interval,
    "ObsoleteInterval" interval NOT NULL,
    "RepeatStrategy" text,
    "MaxRepeatCount" integer NOT NULL,
    "FallbackJobId" text,
    "FallbackStrategyType" integer,
    "ParentJobId" text,
    CONSTRAINT "PK_horarium.jobs" PRIMARY KEY ("JobId")
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_horarium.jobs_JobKey"
    ON public."horarium.jobs" USING btree
    ("JobKey" ASC NULLS LAST);
    
CREATE INDEX IF NOT EXISTS "IX_horarium.jobs_ParentJobId_StartAt_Status"
    ON public."horarium.jobs" USING btree
    ("ParentJobId" ASC NULLS LAST, "StartAt" ASC NULLS LAST, "Status" ASC NULLS LAST);
    
CREATE INDEX IF NOT EXISTS "IX_horarium.jobs_ParentJobId_StartedExecuting_Status"
    ON public."horarium.jobs" USING btree
    ("ParentJobId" ASC NULLS LAST, "StartedExecuting" ASC NULLS LAST, "Status" ASC NULLS LAST);