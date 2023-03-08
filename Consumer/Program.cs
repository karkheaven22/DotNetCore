using _2C2P.Kafka.Domain.Models;
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry.Serdes;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using UniversalLogger.Extensions.LogContext;

namespace Consumer
{
    static class Program
    {
        static void Main(string[] args)
        {
            string brokerList = "localhost:9092";
            Run2(brokerList);
        }

        public static void Run(string brokerList)
        {
            bool enableAutoCommit = false;
            var config = new ConsumerConfig
            {
                GroupId = "advanced-csharp-consumer2",
                BootstrapServers = "localhost:9092",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = enableAutoCommit,
                EnableAutoOffsetStore = false,
                //SecurityProtocol = SecurityProtocol.Ssl
            };

            using var consumer = new ConsumerBuilder<Ignore, string>(config)
                .SetErrorHandler((_, e) => Console.WriteLine($"Error: {e.Reason}"))
                .SetPartitionsAssignedHandler((obj, partitions) =>
                    Console.WriteLine($"Assigned partitions: [{string.Join(", ", partitions)}]"))
                .Build();

            try
            {
                consumer.Subscribe("my-topic2");
                Console.WriteLine("Consumer Subscribe my-topic2...\n");

                CancellationTokenSource cts = new();
                Console.CancelKeyPress += (_, e) =>
                {
                    e.Cancel = true; // prevent the process from terminating.
                    cts.Cancel();
                };

                while (true)
                {
                    try
                    {
                        var result = consumer.Consume(cts.Token);
                        Console.WriteLine($"Consumed event with key {result.Message.Key} and value {result.Message.Value}");

                        consumer.Commit(result);
                        consumer.StoreOffset(result);
                    }
                    catch (ConsumeException e)
                    {
                        Console.WriteLine($"Error occured: {e.Error.Reason}");
                    }
                }

            }
            finally
            {
                consumer.Close();
            }
        }

        public static void Run2(string brokerList)
        {
            bool enableAutoCommit = false;
            var config = new ConsumerConfig
            {
                GroupId = "PGWContext_ES_consumer",
                BootstrapServers = "b-1.2c2puatkafka.p5dufp.c3.kafka.ap-southeast-1.amazonaws.com:9092",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = enableAutoCommit,
                EnableAutoOffsetStore = false,
                //SecurityProtocol = SecurityProtocol.Ssl
            };

            var jsonSerializerConfig = new JsonSerializerOptions { AllowTrailingCommas = true };

            using var consumer = new ConsumerBuilder<string, KafkaLogEvent>(config)
                .SetErrorHandler((_, e) => Console.WriteLine($"Error: {e.Reason}"))
                .SetPartitionsAssignedHandler((obj, partitions) =>
                    Console.WriteLine($"Assigned partitions: [{string.Join(", ", partitions)}]"))
                .SetValueDeserializer(new KafkaJsonSerializer<KafkaLogEvent>(jsonSerializerConfig))
                .Build();

            try
            {
                consumer.Subscribe("PGWContext_ES_log");
                Console.WriteLine("Consumer Subscribe 2c2p-uat-kafka...\n");

                CancellationTokenSource cts = new();
                Console.CancelKeyPress += (_, e) =>
                {
                    e.Cancel = true; // prevent the process from terminating.
                    cts.Cancel();
                };

                while (true)
                {
                    try
                    {
                        ConsumeResult<string, KafkaLogEvent>? result = consumer.Consume(cts.Token);
                        var data = result.Message.Value;
                        Console.WriteLine($"Consumed event with key {result.Message.Key} and value {data.Message} Exception: {data.ExceptionMessage}");
                        if (!string.IsNullOrWhiteSpace(data.ExceptionMessage))
                        {
                            Exception exception2 = new Exception(data.ExceptionMessage);
                            Type exceptionType = typeof(Exception);
                            ConstructorInfo constructorInfo = exceptionType.GetConstructor(new[] { typeof(string) });
                            Exception exception = (Exception)constructorInfo.Invoke(new object[] { data.ExceptionMessage });
                        }
                        //consumer.Commit(result);
                        //consumer.StoreOffset(result);
                    }
                    catch (ConsumeException e)
                    {
                        Console.WriteLine($"Error occured: {e.Error.Reason}");
                    }
                }

            }
            finally
            {
                consumer.Close();
            }
        }
    }
}