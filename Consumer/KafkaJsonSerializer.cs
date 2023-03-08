using Confluent.Kafka;
using System.Text.Json;

namespace Consumer
{
    public class KafkaJsonSerializer<T> : ISerializer<T>, IDeserializer<T>
    {
        private JsonSerializerOptions DefaultOptions { get; }

        public KafkaJsonSerializer(JsonSerializerOptions defaultOptions)
            => DefaultOptions = defaultOptions;

        public byte[] Serialize(T data, SerializationContext context)
            => JsonSerializer.SerializeToUtf8Bytes(data, DefaultOptions);

        public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
            => JsonSerializer.Deserialize<T>(data, DefaultOptions)!;
    }
}
