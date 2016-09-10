﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DQueue.Interfaces;

namespace DQueue.CmdTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var consumer = new QueueConsumer<SampleMessage>(10);

            consumer.Receive((context) =>
            {
                Thread.Sleep(2000);

                if (!context.CancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine(string.Format("[Receiver 1, ThreadID {0}] -> {1}", Task.CurrentId, context.Message.Text));
                }
            });

            consumer.Receive((context) =>
            {
                Thread.Sleep(3000);

                if (!context.CancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine(string.Format("[Receiver 2, ThreadID {0}] -> {1}", Task.CurrentId, context.Message.Text));
                }
            });

            consumer.OnComplete((context) =>
            {
                foreach (var ex in context.Exceptions)
                {
                    Console.WriteLine(ex.Message);
                }
            });

            consumer.OnTimeout((context) =>
            {
                Console.WriteLine("timeout: " + context.Message.Text);
            });


            var producer = new QueueProducer();
            producer.IgnoreHash = true;

            //foreach (var i in Enumerable.Range(0, 10))
            //{
            //    var msg = new SampleMessage
            //    {
            //        Text = "m" + i.ToString()
            //    };

            //    producer.Send(msg);

            //    Console.WriteLine(string.Format("[send] -> {0}", msg.Text));
            //}

            while (true)
            {
                var text = Console.ReadLine();

                if (text == "exit")
                {
                    break;
                }

                producer.Send(new SampleMessage
                {
                    Text = text
                });

                Console.WriteLine(string.Format("[send] -> {0}", text));
            }

            consumer.Dispose();
        }
    }

    public class SampleMessage : IQueueMessage
    {
        public string QueueName
        {
            get
            {
                return "TestQueue";
            }
        }

        public string Text { get; set; }
    }
}
