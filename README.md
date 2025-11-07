# Real-Time Data Processing System with Azure

This project implements a scalable, real-time data processing system using Azure Functions, Azure Event Hubs, and Azure SQL DB. It is designed to meet the requirements of the practicum by providing a robust and observable data pipeline.

## Practicum Requirements Coverage

Here's how the project addresses each of the specified requirements:

- **a) Azure App Service to host the application:**
  - The project is built as an Azure Functions App, which runs on the Azure App Service platform. This provides a managed, serverless environment for the data processing code.

- **b) Azure SQL DB and Event Hubs setup:**
  - The `EventHubProcessorFunction` is configured with an `EventHubTrigger` to automatically ingest data from an Event Hub named `realtime-eventhub-practicum`.
  - The function uses `System.Data.SqlClient` to connect to an Azure SQL DB instance and store the processed data. Connection strings for both services are managed through application settings.

- **c) Autoscaling for Azure App Service:**
  - While the Function App scales based on its own metrics, the underlying App Service Plan can be configured in the Azure Portal with autoscaling rules based on CPU usage and other metrics to handle sustained high-load scenarios.

- **d) Autoscaling for Azure Functions:**
  - The `host.json` file has been configured to enable dynamic scaling of the function instances. The `functionApp.scaling` settings control the minimum and maximum number of instances the app can scale to, ensuring resources are allocated dynamically based on the volume of incoming data.
    ```json
    "scaling": {
      "maxInstances": 10,
      "minInstances": 1
    }
    ```

- **e) Scalable data processing pipeline:**
  - The project implements a complete pipeline:
    1.  **Ingestion:** The `SimulatorTriggerFunction` is an HTTP-triggered function that generates and sends a high volume of events to an Event Hub.
    2.  **Processing:** The `EventHubProcessorFunction` is triggered by new events in the Event Hub, processing them in batches.
    3.  **Analysis & Storage:** The function inserts the event data into an Azure SQL database for later analysis.

- **f) Store and query data using Azure SQL DB:**
  - The `EventHubProcessorFunction` persists each message into a `ProcessedData` table in Azure SQL DB. This allows for efficient querying and analysis of the processed data using standard SQL.

- **g) Simulate heavy traffic loads:**
  - The `SimulatorTriggerFunction` is designed specifically for load testing. It can be triggered via an HTTP request and can send up to 10,000 events in a single run. The number of events is configurable via a query parameter (e.g., `.../api/SimulatorTriggerFunction?count=5000`).

- **h) Monitor performance and resource utilization:**
  - The project is integrated with Application Insights. The `Program.cs` and `host.json` files are configured to send telemetry, performance metrics, and logs to Application Insights. This allows for real-time monitoring of CPU usage, function execution rates, latency, and error rates in the Azure Portal.

- **i) Optimize configuration settings:**
  - The `host.json` file includes optimized settings for the Event Hubs extension. `maxBatchSize` and `prefetchCount` are configured to fine-tune how the function receives events, which helps in managing memory and execution time, thereby minimizing latency and maximizing resource utilization.

## Project Setup

### Prerequisites

- .NET 8 SDK
- Azure Functions Core Tools
- An Azure Subscription

### Azure Services Required

1.  **Azure Event Hubs Namespace:** With an Event Hub named `realtime-eventhub-practicum`.
2.  **Azure SQL DB:** A server and a database.
3.  **Azure Application Insights:** For monitoring.

### Configuration

1. **Create a `local.settings.json` file** in the `EventHubProcessor/EventHubProcessor` directory with the following structure, filling in the actual connection strings:

    ```json
    {
      "IsEncrypted": false,
      "Values": {
        "AzureWebJobsStorage": "UseDevelopmentStorage=true",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
        "EventHubName": "realtime-eventhub-practicum",
    
        "EventHubConnectionString": "EVENT_HUB_CONNECTION_STRING",
        "EventHubConnection": "EVENT_HUB_CONNECTION",
        "SqlConnectionString": "SQL_DB_CONNECTION_STRING"
      }
    }
    ```

2. **Set up the SQL Database Table.**
    Execute the following SQL script in Azure SQL DB to create the necessary table:

    ```sql
    CREATE TABLE ProcessedData (
        Id INT PRIMARY KEY IDENTITY(1,1),
        MessageContent NVARCHAR(MAX) NOT NULL,
        ProcessedTimestamp DATETIME2 NOT NULL
    );
    ```

## Running and Testing

1.  **Run Locally:**
    - Navigate to the `EventHubProcessor/EventHubProcessor` directory in terminal.
    - Run `func start` to start the local Functions host.

2.  **Simulate Load:**
    - Use a tool like `curl` or web browser to send a POST or GET request to the `SimulatorTriggerFunction` endpoint provided by the Functions host.
    - To generate 5,000 events, use the URL: `http://localhost:7071/api/SimulatorTriggerFunction?count=5000`

3.  **Monitor in Azure:**
    - Once deployed to Azure, open the Application Insights resource associated with Function App.
    - Use the **Live Metrics** view to see real-time telemetry as you run the simulation.
    - Observe the **Performance** and **Failures** blades to analyze execution time, throughput, and any errors.
    - In the Function App's **Monitor** section, you can see the invocation logs and observe how the app scales out based on the load.
