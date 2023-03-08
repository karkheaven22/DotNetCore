using _2C2P.Kafka.Domain.Models;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Confluent.SchemaRegistry;
using Consumer;
using LogHelper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UniversalLogger.Extensions.LogContext;
using UniversalLogger.Extensions.Logger;
using static Confluent.Kafka.ConfigPropertyNames;
using static Org.BouncyCastle.Math.EC.ECCurve;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace DotNetCore.Controllers
{
    class User
    {
        [JsonRequired]
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonRequired]
        [JsonProperty("favorite_color")]
        public string FavoriteColor { get; set; }

        [JsonProperty("favorite_number")]
        public long FavoriteNumber { get; set; }
    }

    [ApiController]
    [Route("api/[Controller]")]
    public class KafkaController : Controller
    {
        [HttpGet("Push")]
        public async Task Push()
        {
            var config = new ProducerConfig { 
                BootstrapServers = "b-2.2c2puatkafka.p5dufp.c3.kafka.ap-southeast-1.amazonaws.com:9094,b-1.2c2puatkafka.p5dufp.c3.kafka.ap-southeast-1.amazonaws.com:9094",
                ReconnectBackoffMaxMs = 10000, //10 second
                EnableIdempotence = true,
                MessageSendMaxRetries = 5, //Maximum Retries 5 times
                MessageTimeoutMs = 3000 //Retries 1Minute
            };

            Action<DeliveryReport<string, string>> handler =
                r => Console.WriteLine(!r.Error.IsError ? $"Delivered message to {r.TopicPartitionOffset}" : $"Delivery Error: {r.Error.Reason}");


            //using var p = new ProducerBuilder<string, string>(config).Build();
            ProducerConfig myOptions = config;
            var p = new _2C2P.Kafka.Helper.KafkaProducer(myOptions);
            try
            {
                string text;
                Console.WriteLine($"Please Key someting");
                while ((text = Console.ReadLine()) != "q")
                {
                    try
                    {
                        //p.Send("my-topic2",  text, handler);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"error producing message: {e.Message}");
                    }
                }

                //p.Flush(TimeSpan.FromSeconds(10));
            }
            catch (ProduceException<string, ApplicationUser> e)
            {
                Console.WriteLine($"Delivery failed: {e.Error.Reason}");
            }
        }

        [HttpGet("Pull")]
        public async Task Pull()
        {

            var config = new ConsumerConfig
            {
                GroupId = "test-consumer-group",
                BootstrapServers = "localhost:9092",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            using (var c = new ConsumerBuilder<string, string>(config)
                .SetKeyDeserializer(Deserializers.Utf8)
                .SetErrorHandler((_, e) => Console.WriteLine($"Error: {e.Reason}"))
                .Build())
            {
                c.Subscribe("my-topic2");

                CancellationTokenSource cts = new CancellationTokenSource();
                Console.CancelKeyPress += (_, e) =>
                {
                    e.Cancel = true; // prevent the process from terminating.
                    cts.Cancel();
                };

                try
                {
                    while (true)
                    {
                        try
                        {
                            var cr = c.Consume(cts.Token);
                            Console.WriteLine($"Consumed message '{cr.Message.Value}' at: '{cr.TopicPartitionOffset}'.");
                        }
                        catch (ConsumeException e)
                        {
                            Console.WriteLine($"Error occured: {e.Error.Reason}");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Ensure the consumer leaves the group cleanly and final offsets are committed.
                    c.Close();
                }
            }
        }

        [HttpGet("Run")]
        public async Task Run()
        {
            string bootstrapServers = "localhost:9092";
            string schemaRegistryUrl = "localhost:8081";
            string topicName = "my-topic";

            var producerConfig = new ProducerConfig
            {
                BootstrapServers = bootstrapServers
            };

            var schemaRegistryConfig = new SchemaRegistryConfig
            {
                // Note: you can specify more than one schema registry url using the
                // schema.registry.url property for redundancy (comma separated list). 
                // The property name is not plural to follow the convention set by
                // the Java implementation.
                Url = schemaRegistryUrl
            };

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = bootstrapServers,
                GroupId = "json-example-consumer-group"
            };

            // Note: Specifying json serializer configuration is optional.
            var jsonSerializerConfig = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
            };

            CancellationTokenSource cts = new CancellationTokenSource();
            var consumeTask = Task.Run(() =>
            {
                using (var consumer =
                    new ConsumerBuilder<string, User>(consumerConfig)
                        //.SetValueDeserializer(new UniversalLogger.Kafka.KafkaJsonSerializer<User>(jsonSerializerConfig))
                        .SetErrorHandler((_, e) => Console.WriteLine($"Error: {e.Reason}"))
                        .Build())
                {
                    consumer.Subscribe(topicName);

                    try
                    {
                        while (true)
                        {
                            try
                            {
                                var cr = consumer.Consume(cts.Token);
                                var user = cr.Message.Value;
                                Console.WriteLine($"user name: {user.Name}, favorite number: {user.FavoriteNumber}, favorite color: {user.FavoriteColor}");
                            }
                            catch (ConsumeException e)
                            {
                                Console.WriteLine($"Consume error: {e.Error.Reason}");
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        consumer.Close();
                    }
                }
            });

            using (var schemaRegistry = new CachedSchemaRegistryClient(schemaRegistryConfig))
            using (var producer =
                new ProducerBuilder<string, User>(producerConfig)
                    //.SetValueSerializer(new UniversalLogger.Kafka.KafkaJsonSerializer<User>(jsonSerializerConfig))
                    .Build())
            {
                Console.WriteLine($"{producer.Name} producing on {topicName}. Enter first names, q to exit.");

                long i = 1;
                string text;
                while ((text = Console.ReadLine()) != "q")
                {
                    User user = new User { Name = text, FavoriteColor = "blue", FavoriteNumber = i++ };
                    try
                    {
                        await producer.ProduceAsync(topicName, new Message<string, User> { Value = user });
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"error producing message: {e.Message}");
                    }
                }
            }

            cts.Cancel();

            //using (var schemaRegistry = new CachedSchemaRegistryClient(schemaRegistryConfig))
            //{
            //    // Note: a subject name strategy was not configured, so the default "Topic" was used.
            //    var schema = await schemaRegistry.GetLatestSchemaAsync(SubjectNameStrategy.Topic.ConstructValueSubjectName(topicName));
            //    Console.WriteLine("\nThe JSON schema corresponding to the written data:");
            //    Console.WriteLine(schema.SchemaString);
            //}
        }

        //[HttpGet("PGWContextRun")]
        //public async Task PGWContextRun()
        //{
        //    string bootstrapServers = "b-1.2c2puatkafka.p5dufp.c3.kafka.ap-southeast-1.amazonaws.com:9092";
        //    string topicName = "PgwContext-uat-topic";

        //    var producerConfig = new ProducerConfig
        //    {
        //        BootstrapServers = bootstrapServers,
        //        ReconnectBackoffMaxMs = 10000, //10 second
        //        EnableIdempotence = false,
        //        MessageSendMaxRetries = 5, //Maximum Retries 5 times
        //        MessageTimeoutMs = 3000, //Retries 1Minute
        //        Acks = Acks.Leader,
        //    };

        //    Action<DeliveryReport<string, PGWContext>> handler =
        //        r => Console.WriteLine(!r.Error.IsError ? $"Delivered message to {r.TopicPartitionOffset}" : $"Delivery Error: {r.Error.Reason}");

        //    // Note: Specifying json serializer configuration is optional.
        //    //var jsonSerializerConfig = new JsonSerializerOptions { AllowTrailingCommas = true };
        //    //using var producer =
        //    //    new ProducerBuilder<string, PGWContext>(producerConfig)
        //    //        .SetValueSerializer(new KafkaJsonSerializer<PGWContext>(jsonSerializerConfig))
        //    //.Build();


        //    ProducerConfig myOptions = producerConfig;
        //    var p = new _2C2P.Kafka.Helper.KafkaProducer(myOptions);

        //    Console.WriteLine($"{topicName} Start");

        //    while (true)
        //    {
        //        PGWContext data = new PGWContext
        //        {
        //            ElapsedTimeStart = DateTime.Now,
        //            Level = UniversalLogger.Enums.Level.Info,
        //            Message = $"Message : {DateTime.Now:yyyy-MM-dd HH:mm:ss}"
        //        };
        //        try
        //        {
        //            await p.SendAsync<PGWContext>(topicName, data);
        //            //producer.Produce(topicName, new Message<string, PGWContext> { Value = data }, handler);
        //        }
        //        catch (KafkaRetriableException e)
        //        {
        //            Console.WriteLine($"error producing message: {e.Message}");
        //        }
        //        catch (Exception e)
        //        {
        //            Console.WriteLine($"error producing message: {e.Message}");
        //        }

        //        Thread.Sleep(5000);
        //    }
        //}
        [HttpGet("PGWContext_TestRun")]
        public async Task PGWContext_Test()
        {
            PGWContext data = new PGWContext
            {
                Level = UniversalLogger.Enums.Level.Info,
                Message = $"Message : {DateTime.Now:yyyy-MM-dd HH:mm:ss}"
            };

            await PGWLogger.Default.LogPGWContextAsync(data, true, null);
        }

        [HttpGet("StandardContextRun")]
        public async Task StandardContextRun()
        {
            string bootstrapServers = "b-1.2c2puatkafka.p5dufp.c3.kafka.ap-southeast-1.amazonaws.com:9092";
            string topicName = "StandardContext";

            var producerConfig = new ProducerConfig
            {
                BootstrapServers = bootstrapServers,
                //ReconnectBackoffMaxMs = 10000, //10 second
                //EnableIdempotence = false,
                //MessageSendMaxRetries = 5, //Maximum Retries 5 times
                //MessageTimeoutMs = 3000, //Retries 1Minute
                Acks = Acks.Leader,
                SaslMechanism = SaslMechanism.ScramSha512,
                SaslUsername = "AmazonMSK_Dev",
                SaslPassword = "kd34nREr43lk03pcaT5pzb36ikOY43kaocRe31ee",
                SecurityProtocol = SecurityProtocol.SaslSsl
            };

            Action<DeliveryReport<string, string>> handler =
                r => Console.WriteLine(!r.Error.IsError ? $"Delivered message to {r.TopicPartitionOffset}" : $"Delivery Error: {r.Error.Reason}");

            // Note: Specifying json serializer configuration is optional.
            //var jsonSerializerConfig = new JsonSerializerOptions { AllowTrailingCommas = true };
            //using var producer =
            //    new ProducerBuilder<string, PGWContext>(producerConfig)
            //        .SetValueSerializer(new KafkaJsonSerializer<PGWContext>(jsonSerializerConfig))
            //.Build();


            ProducerConfig myOptions = producerConfig;
            var p = new _2C2P.Kafka.Helper.KafkaClient(myOptions, null);

            Console.WriteLine($"{topicName} Start");

            while (true)
            {
                

                var message = new KafkaLogEvent()
                {
                    ContextName = "StandardContext",
                    Message = JsonSerializer.Serialize(new StandardContext
                    {
                        Level = UniversalLogger.Enums.Level.Info,
                        Message = $"Message : {DateTime.Now:yyyy-MM-dd HH:mm:ss}"
                    }),
                    ExceptionMessage = ""
                };

                try
                {
                    var result = await p.SendAsync(topicName, message);
                    //if(result != null)
                    //    Console.WriteLine($"Send producing message: {result.TopicPartitionOffset}");
                    //p.Send<StandardContext>(topicName, data, handler);
                    //producer.Produce(topicName, new Message<string, PGWContext> { Value = data }, handler);
                }
                catch (ProduceException<string, string> ex)
                {
                    Console.WriteLine($"error producing message: {ex.Message}");
                    //throw;
                }
                catch (KafkaRetriableException e)
                {
                    Console.WriteLine($"error producing message: {e.Message}");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"error producing message: {e.Message}");
                }

                Thread.Sleep(5000);
            }
        }

        [HttpPost("ListGroups")]
        public void ListGroups(string bootstrapServers = "b-1.2c2puatkafka.p5dufp.c3.kafka.ap-southeast-1.amazonaws.com:9092")
        {
            using var adminClient = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = bootstrapServers }).Build();
            // Warning: The API for this functionality is subject to change.
            var groups = adminClient.ListGroups(TimeSpan.FromSeconds(10));
            Console.WriteLine($"Consumer Groups:");
            foreach (var g in groups)
            {
                Console.WriteLine($"  Group: {g.Group} {g.Error} {g.State}");
                Console.WriteLine($"  Broker: {g.Broker.BrokerId} {g.Broker.Host}:{g.Broker.Port}");
                Console.WriteLine($"  Protocol: {g.ProtocolType} {g.Protocol}");
                Console.WriteLine($"  Members:");
                foreach (var m in g.Members)
                {
                    Console.WriteLine($"    {m.MemberId} {m.ClientId} {m.ClientHost}");
                    Console.WriteLine($"    Metadata: {m.MemberMetadata.Length} bytes");
                    Console.WriteLine($"    Assignment: {m.MemberAssignment.Length} bytes");
                }
            }
        }

        protected string ToString(int[] array) => $"[{string.Join(", ", array)}]";

        [HttpPost("PrintMetadata")]
        public void PrintMetadata(string bootstrapServers = "b-1.2c2puatkafka.p5dufp.c3.kafka.ap-southeast-1.amazonaws.com:9092")
        {
            using var adminClient = new AdminClientBuilder(new AdminClientConfig { 
                BootstrapServers = bootstrapServers,
                //SaslMechanism = SaslMechanism.ScramSha512,
                //SaslUsername = "AmazonMSK_Dev",
                //SaslPassword = "kd34nREr43lk03pcaT5pzb36ikOY43kaocRe31ee",
                //SecurityProtocol = SecurityProtocol.SaslSsl
            }).Build();
            // Warning: The API for this functionality is subject to change.
            var meta = adminClient.GetMetadata(TimeSpan.FromSeconds(20));
            Log.Info($"{meta.OriginatingBrokerId} {meta.OriginatingBrokerName}");

            meta.Brokers.ForEach(broker => Log.Info($"Broker: {broker.BrokerId} {broker.Host}:{broker.Port}"));

            meta.Topics.ForEach(topic =>
            {
                Log.Info($"Topic: {topic.Topic} {topic.Error}");
                topic.Partitions.ForEach(partition =>
                {
                    Log.Info($"Partition: {partition.PartitionId} Replicas: {ToString(partition.Replicas)} InSyncReplicas: {ToString(partition.InSyncReplicas)}");
                });
            });
        }

        [HttpPost("CreateTopicAsync")]
        public async Task CreateTopicAsync(string topic, string bootstrapServers = "b-1.2c2puatkafka.p5dufp.c3.kafka.ap-southeast-1.amazonaws.com:9092")
        {
            using var adminClient = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = bootstrapServers }).Build();
            try
            {
                await adminClient.CreateTopicsAsync(new TopicSpecification[] {
                        new TopicSpecification { Name = topic, ReplicationFactor = 2, NumPartitions = 1 }
                    });
                Log.Info($"Topic {topic} create successfully");
            }
            catch (CreateTopicsException e)
            {
                Log.Error($"An error occurred creating topic {e.Results[0].Topic}: {e.Results[0].Error.Reason}");
            }
        }

        [HttpPost("DeleteTopicsAsync")]
        public async Task DeleteTopicsAsync(string topic, string bootstrapServers = "b-1.2c2puatkafka.p5dufp.c3.kafka.ap-southeast-1.amazonaws.com:9092")
        {
            using var adminClient = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = bootstrapServers }).Build();
            try
            {
                await adminClient.DeleteTopicsAsync(new List<string> { topic });
                Log.Info($"Topic {topic} deleted successfully");
            }
            catch (DeleteTopicsException e)
            {
                if (e.Results.Select(r => r.Error.Code).Any(el => el != ErrorCode.UnknownTopicOrPart && el != ErrorCode.NoError))
                    Log.Error($"Unable to delete topics {topic}");
            }
        }
    }
}
