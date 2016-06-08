# DQueue
A message queue clients wrapper with multiple threads supported. Queue clients support Redis and RabbitMQ.

Sample Configuration
------------
```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <appSettings>
    <add key="QueueProvider" value="Redis" />
  </appSettings>
  <connectionStrings>
    <add name="Redis_Connection" connectionString="127.0.0.1:6379,password=,allowAdmin=true" />
    <add name="RabbitMQ_Connection" connectionString="HostName=localhost,UserName=rulee,Password=abc123" />
  </connectionStrings>
</configuration>
```

Sample Message
------------
```c#
public class SampleMessage : IQueueMessage
{
  public string QueueName
  {
    get { return "TestQueue"; }
  }

  public string Text { get; set; }
}
```

Sample Producer
------------
```c#
var producer = new QueueProducer();

producer.Send(new SampleMessage { Text = "test" });
```

Sample Consumer
------------
```c#
// specified 10  threads on receiving queue message
var consumer = new QueueConsumer<SampleMessage>(10);

consumer.Receive((context) =>
{
  // handler thread 1
});

consumer.Receive((context) =>
{
  // handler thread 2
});

consumer.Complete((context) =>
{
  // will goes here when "handler thread 1" and "handler thread 2" are done
  
  foreach (var ex in context.Exceptions)
  {
    Console.WriteLine(ex.Message);
  }
});
```

The samples above to give you the first impression of this component. For more details please turn to wiki.
