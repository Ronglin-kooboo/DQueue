﻿namespace DQueue
{
    public static class Constants
    {
        public static readonly string EnqueueTimeField = "$EnqueueTime$";
        public static readonly string DequeueLockerFlag = "$DequeueLocker$";
        public static readonly string PoolingLockerFlag = "$PoolingLocker$";
        public static readonly string ProcessingQueueName = "-$Processing$";

        public static readonly QueueProvider DefaultProvider = QueueProvider.Configured;
        public static readonly int DefaultTimeoutMilliseconds = 1000 * 60 * 2; // 2 minutes
        public static readonly int DefaultMaxParallelThreads = 50;
        public static readonly bool RetryOnTimeout = false;
    }
}
