# Email Service Azure Function

This Azure Function processes email requests from a Service Bus queue and sends emails using SMTP. It includes comprehensive monitoring through Application Insights.

## Features

- Processes JSON messages from Azure Service Bus queue
- Sends emails via SMTP
- Comprehensive error handling with dead-letter queue support
- Application Insights monitoring
- Automated deployment with GitHub Actions

## Prerequisites

- Azure subscription
- Azure Service Bus namespace and queue
- SMTP server credentials
- GitHub account (for CI/CD)
- .NET 9.0 SDK (for local development)
- Azure Functions Core Tools

## Message Format

The Service Bus queue expects JSON messages in the following format:

```json
{
    "recipientAddress": "recipient@example.com",
    "subject": "Email Subject",
    "body": "Email Body Content"
}
```

## Configuration

### Required Application Settings

Configure the following settings in your `local.settings.json` for local development, or in Azure Function App settings for production:

```json
{
    "ServiceBusConnection": "Service Bus connection string",
    "QueueName": "email-queue",
    "SMTP_HOST": "smtp.example.com",
    "SMTP_PORT": "587",
    "SMTP_USERNAME": "your-username",
    "SMTP_PASSWORD": "your-password",
    "FROM_ADDRESS": "noreply@yourdomain.com",
    "APPLICATIONINSIGHTS_CONNECTION_STRING": "Application Insights connection string"
}
```

### Local Development

1. Clone the repository
2. Create `local.settings.json` and update with your values
3. Install dependencies:
   ```bash
   dotnet restore
   ```
4. Run the function:
   ```bash
   func start
   ```

## Deployment

### Azure Resources Setup

1. Create an Azure Function App:
   ```bash
   az functionapp create \
       --name <app-name> \
       --resource-group <resource-group> \
       --consumption-plan-location <location> \
       --runtime dotnet-isolated \
       --functions-version 4
   ```

2. Create an Application Insights resource:
   ```bash
   az monitor app-insights component create \
       --app <app-name> \
       --location <location> \
       --resource-group <resource-group>
   ```

### GitHub Actions Deployment

1. Get the Function App publish profile from Azure Portal
2. Add it as a GitHub secret named `AZURE_FUNCTIONAPP_PUBLISH_PROFILE`
3. Update the Function App name in `.github/workflows/deploy-function.yml`
4. Push to the main branch to trigger deployment

## Monitoring

The function includes Application Insights monitoring for:

### Tracked Events
- `MessageReceived`: When a message is received from the queue
- `EmailSent`: When an email is successfully sent

### Exception Tracking
- JSON deserialization errors
- SMTP sending failures
- Other unhandled exceptions

### Metrics
- Message processing duration
- Success/failure rates
- Queue length
- SMTP server response times

View these metrics in the Azure Portal under the Application Insights resource.

## Error Handling

The function implements a robust error handling strategy:

- Invalid JSON messages are moved to the dead-letter queue
- SMTP failures trigger the Azure Functions retry policy
- All errors are logged to Application Insights with context
- Stack traces and error details are preserved for debugging

## Project Structure

```
├── EmailProcessor.cs          # Main function code
├── EmailProcessor.csproj      # Project file with dependencies
├── host.json                 # Function host configuration
├── local.settings.json       # Local development settings
└── .github
    └── workflows
        └── deploy-function.yml  # CI/CD pipeline
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.
