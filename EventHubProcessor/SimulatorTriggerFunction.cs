using System.Net;
using System.Text;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventHubProcessor;

public class SimulatorTriggerFunction
{
    private readonly ILogger<SimulatorTriggerFunction> _logger;
    private readonly Settings _settings;

    public SimulatorTriggerFunction(ILogger<SimulatorTriggerFunction> logger, IOptions<Settings> options)
    {
        _logger = logger;
        _settings = options.Value;
    }

    [Function("SimulatorTriggerFunction")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request to start simulation.");

        if (string.IsNullOrEmpty(_settings.EventHubConnectionString))
        {
            _logger.LogError("'EventHubConnectionString' is not set in application settings.");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("Configuration error: Event Hub connection string is missing.");
            return errorResponse;
        }

        if (string.IsNullOrEmpty(_settings.EventHubName))
        {
            _logger.LogError("'EventHubName' is not set in application settings.");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("Configuration error: Event Hub name is missing.");
            return errorResponse;
        }

        // Get the number of events to send from the query string, or default to 1000.
        if (!int.TryParse(
                req.Url.Query.Split('&').FirstOrDefault(q => q.StartsWith("count="))?.Split('=').LastOrDefault(),
                out var eventCount)) eventCount = 1000;

        if (eventCount > 10000) eventCount = 10000;

        _logger.LogInformation($"Starting simulation to send {eventCount} events to '{_settings.EventHubName}'.");

        try
        {
            await using (var producerClient =
                         new EventHubProducerClient(_settings.EventHubConnectionString, _settings.EventHubName))
            {
                var totalSentCount = 0;
                while (totalSentCount < eventCount)
                {
                    using var eventBatch = await producerClient.CreateBatchAsync();
                    var batchCount = 0;
                    while (totalSentCount < eventCount && batchCount < 250) // Batch up to 250 events
                    {
                        var eventBody = Encoding.UTF8.GetBytes($"Event {totalSentCount + 1}");
                        if (eventBatch.TryAdd(new EventData(eventBody)))
                        {
                            totalSentCount++;
                            batchCount++;
                        }
                        else
                        {
                            // If the batch is full, send it and create a new one.
                            break;
                        }
                    }

                    if (eventBatch.Count > 0)
                    {
                        await producerClient.SendAsync(eventBatch);
                        _logger.LogInformation($"Sent a batch of {eventBatch.Count} events.");
                    }
                }

                _logger.LogInformation(
                    $"A total of {totalSentCount} events have been published to {_settings.EventHubName}.");
            }

            var okResponse = req.CreateResponse(HttpStatusCode.OK);
            await okResponse.WriteStringAsync(
                $"Simulation finished. {eventCount} events sent to {_settings.EventHubName}.");
            return okResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing events to {EventHubName}: {ExMessage}", _settings.EventHubName,
                ex.Message);
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("An unexpected error occurred while sending events.");
            return errorResponse;
        }
    }
}