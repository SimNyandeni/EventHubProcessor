using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace EventHubProcessor;

public class EventHubProcessorFunction
{
    private readonly ILogger<EventHubProcessorFunction> _logger;
    private readonly Settings _settings;

    public EventHubProcessorFunction(ILogger<EventHubProcessorFunction> logger, IOptions<Settings> options)
    {
        _logger = logger;
        _settings = options.Value;
    }

    [Function("EventHubProcessorFunction")]
    public async Task Run([EventHubTrigger("realtime-eventhub-practicum", Connection = "EventHubConnectionString")] string[] messages)
    {
        var exceptions = new List<Exception>();

        if (string.IsNullOrEmpty(_settings.SqlConnectionString))
        {
            _logger.LogError("SQL connection string is not set. Please configure 'SqlConnectionString' in application settings.");
            throw new InvalidOperationException("SQL connection string is not configured.");
        }

        // Open the SQL connection once per batch of messages for efficiency.
        await using (var connection = new SqlConnection(_settings.SqlConnectionString))
        {
            await connection.OpenAsync();

            foreach (string messageBody in messages)
            {
                try
                {
                    _logger.LogInformation($"C# Event Hub trigger function processed a message: {messageBody}");

                    const string sql = "INSERT INTO ProcessedData (MessageContent, ProcessedTimestamp) VALUES (@MessageContent, @ProcessedTimestamp)";
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@MessageContent", messageBody);
                        command.Parameters.AddWithValue("@ProcessedTimestamp", DateTime.UtcNow);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                catch (Exception e)
                {
                    // Capture the exception and continue processing the rest of the batch.
                    _logger.LogError($"Error processing message: '{messageBody}'. Exception: {e.Message}");
                    exceptions.Add(new Exception($"Failed to process message: {messageBody}", e));
                }
            }
        }

        if (exceptions.Count > 0)
        {
            _logger.LogWarning($"{exceptions.Count} out of {messages.Length} messages in the batch failed to process.");
        }
        else
        {
            _logger.LogInformation($"Successfully processed {messages.Length} messages.");
        }

        switch (exceptions.Count)
        {
            // If any exceptions were thrown, re-throw an aggregate exception to mark the entire batch as failed.
            case > 1:
                throw new AggregateException(exceptions);
            case 1:
                throw exceptions.Single();
        }
    }
}