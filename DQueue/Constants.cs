﻿namespace DQueue
{
    internal class Constants
    {
        public static readonly string EnqueueTimeField = "$EnqueueTime$";
        public static readonly string DequeueLockerFlag = "-$DequeueLocker-{0}$";
        public static readonly string PoolingLockerFlag = "-$PoolingLocker-{0}$";
        public static readonly string ProcessingQueueName = "-$Processing-{0}$";

        public static readonly QueueProvider DefaultProvider = QueueProvider.Configured;
        public static readonly string DefaultTimeoutMilliseconds = "0.00:02:00.0000000"; // 2 minutes
        public static readonly int DefaultMaxParallelThreads = 20;
        public static readonly bool RetryOnTimeout = false;
    }
}
