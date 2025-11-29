using Unity.Jobs;

namespace Rayforge.Utility.Threading
{
    /// <summary>
    /// Utility class that provides simplified helper methods for scheduling and completing Unity Jobs.
    /// </summary>
    public static class UnityJobDispatcher
    {
        /// <summary>
        /// Schedules a single <see cref="IJob"/> and returns its <see cref="JobHandle"/>.
        /// </summary>
        /// <typeparam name="T">A struct implementing <see cref="IJob"/>.</typeparam>
        /// <param name="job">The job to schedule.</param>
        /// <returns>The <see cref="JobHandle"/> representing the scheduled job.</returns>
        public static JobHandle Run<T>(T job) 
            where T : struct, IJob
            => job.Schedule();

        /// <summary>
        /// Schedules a <see cref="IJobParallelFor"/> job and returns its <see cref="JobHandle"/>.
        /// </summary>
        /// <typeparam name="T">A struct implementing <see cref="IJobParallelFor"/>.</typeparam>
        /// <param name="job">The job to schedule.</param>
        /// <param name="length">The number of iterations the job should run.</param>
        /// <param name="batchSize">How many iterations should be processed per job batch. Default is 32.</param>
        /// <returns>The <see cref="JobHandle"/> representing the scheduled job.</returns>
        public static JobHandle RunParallelFor<T>(T job, int length, int batchSize = 32) 
            where T : struct, IJobParallelFor
            => job.Schedule(length, batchSize);

        /// <summary>
        /// Schedules and immediately completes a single <see cref="IJob"/>. 
        /// Use this to force synchronous execution on the calling thread.
        /// </summary>
        /// <typeparam name="T">A struct implementing <see cref="IJob"/>.</typeparam>
        /// <param name="job">The job to schedule and complete.</param>
        public static void Complete<T>(T job) 
            where T : struct, IJob
        {
            var jobHandle = job.Schedule();
            jobHandle.Complete();
        }

        /// <summary>
        /// Schedules and immediately completes a <see cref="IJobParallelFor"/>. 
        /// Use this for synchronous execution of parallel jobs.
        /// </summary>
        /// <typeparam name="T">A struct implementing <see cref="IJobParallelFor"/>.</typeparam>
        /// <param name="job">The job to schedule and complete.</param>
        /// <param name="length">The number of iterations the job should run.</param>
        /// <param name="batchSize">How many iterations should be processed per job batch. Default is 32.</param>
        public static void CompleteParallelFor<T>(T job, int length, int batchSize = 32) 
            where T : struct, IJobParallelFor
        {
            var jobHandle = job.Schedule(length, batchSize);
            jobHandle.Complete();
        }
    }
}