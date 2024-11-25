using KafkaConsumerApp;
using KafkaConsumerApp.Data;
using Microsoft.EntityFrameworkCore;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting Kafka Consumer...");

        // Configure DbContext
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Your_Connection_String")
            .Options;

        // Initialize DbContext
        using var dbContext = new AppDbContext(options);
        dbContext.Database.Migrate();

        // Kafka consumer configuration
        var config = new ConsumerConfig
        {
            BootstrapServers = "localhost:9092",
            GroupId = "kafka-consumer-group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        // Abstract Factory for processing messages
        var factory = new MessageProcessorFactory();

        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        consumer.Subscribe(new[] { "Bid", "Trade", "PurchaseAdvice" });

        Console.WriteLine("Listening to Kafka topics...");

        try
        {
            while (true)
            {
                var consumeResult = consumer.Consume();

                Console.WriteLine($"Message received from topic {consumeResult.Topic}: {consumeResult.Message.Value}");

                // Process the message
                var processor = factory.GetProcessor(consumeResult.Topic);
                if (processor != null)
                {
                    await processor.ProcessMessageAsync(consumeResult.Message.Value, dbContext);
                }
                else
                {
                    Console.WriteLine($"No processor available for topic: {consumeResult.Topic}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Consumer closed.");
        }
        finally
        {
            consumer.Close();
        }
    }
}
