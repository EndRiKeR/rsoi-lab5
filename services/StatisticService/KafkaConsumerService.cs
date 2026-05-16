using System.Text.Json;
using Confluent.Kafka;
using StatisticService.Database;
using StatisticService.Database.Models;

namespace StatisticService;

public class KafkaConsumerService : BackgroundService
{
    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;

    public KafkaConsumerService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<KafkaConsumerService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Kafka consumer starting...");

        var config = new ConsumerConfig
        {
            BootstrapServers = "kafka:9092",
            GroupId = "statistics-consumer",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true,
            AllowAutoCreateTopics = true
        };

        try
        {
            using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
            consumer.Subscribe("rsoi-events");

            _logger.LogInformation("Kafka consumer subscribed to rsoi-events");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(TimeSpan.FromSeconds(5));
                    if (result == null) continue;

                    var eventData = JsonSerializer.Deserialize<StatisticEvent>(result.Message.Value);
                    if (eventData != null)
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<StatisticContext>();
                        db.Events.Add(new StatisticsEvent
                        {
                            EventType = eventData.EventType,
                            Timestamp = eventData.Timestamp,
                            Username = eventData.Username,
                            Payload = eventData.Payload
                        });
                        await db.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation("Saved event {EventType} from {Username}", eventData.EventType, eventData.Username);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (ConsumeException ex)
                {
                    _logger.LogWarning(ex, "Kafka consume error, retrying...");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing Kafka message");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Fatal error in Kafka consumer");
        }

        _logger.LogInformation("Kafka consumer stopped");
    }
}

public class StatisticEvent
{
    public string EventType { get; set; }
    public DateTime Timestamp { get; set; }
    public string Username { get; set; }
    public string Payload { get; set; }
}