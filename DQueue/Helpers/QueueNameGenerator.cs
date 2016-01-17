﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DQueue.Interfaces;

namespace DQueue.Helpers
{
    public class QueueNameGenerator
    {
        public static string GetQueueName(Type messageType)
        {
            if (typeof(IQueueMessage).IsAssignableFrom(messageType))
            {
                try
                {
                    var instance = (IQueueMessage)Activator.CreateInstance(messageType);
                    return instance.QueueName;
                }
                catch (Exception)
                {
                }
            }

            return messageType.FullName;
        }

        public static string GetQueueName<TMessage>()
            where TMessage : new()
        {
            var obj = new TMessage();

            var imsg = obj as IQueueMessage;

            if (imsg != null)
            {
                return imsg.QueueName;
            }
            else
            {
                return obj.GetType().FullName;
            }
        }

        public static string GetProcessingQueueName(string associatedQueueName)
        {
            return associatedQueueName + "$processing$";
        }
    }
}
