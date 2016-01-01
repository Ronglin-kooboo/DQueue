﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DQueue.Interfaces;
using DQueue.QueueProviders;

namespace DQueue
{
    public class QueueManager
    {
        public static IQueueProvider Get()
        {
            return new RabbitMQProvider();
        }
    }
}
