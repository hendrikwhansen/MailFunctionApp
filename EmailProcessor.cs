using System;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace EmailProcessor
{
    public class EmailMessage
    {
        public string RecipientAddress { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
    }

    public class Program
    {
        public static void Main()
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices(services =>
                {
                    services.AddApplicationInsightsTelemetryWorkerService();
                    services.ConfigureFunctionsApplicationInsights();
                })
                .Build();

            host.Run();
        }
    }

    public class ServiceBusEmailProcessor
    {
        private readonly ILogger<ServiceBusEmailProcessor> _logger;
        private readonly TelemetryClient _telemetryClient;

        public ServiceBusEmailProcessor(
            ILogger<ServiceBusEmailProcessor> logger,
            TelemetryClient telemetryClient)
        {
            _logger = logger;
            _telemetryClient = telemetryClient;
        }

        [Function("ProcessEmailMessage")]
        public async Task Run(
            [ServiceBusTrigger("%QueueName%", Connection = "ServiceBusConnection")] 
            string messageBody)
        {
            try
            {
                _logger.LogInformation($"Processing message: {messageBody}");
                
                // Track the received message
                _telemetryClient.TrackEvent("MessageReceived", new Dictionary<string, string>
                {
                    { "messageBody", messageBody }
                });
                
                var emailMessage = JsonSerializer.Deserialize<EmailMessage>(messageBody);
                await SendEmailAsync(emailMessage);
                
                // Track successful processing
                _telemetryClient.TrackEvent("EmailSent", new Dictionary<string, string>
                {
                    { "recipient", emailMessage.RecipientAddress },
                    { "subject", emailMessage.Subject }
                });
                
                _logger.LogInformation($"Email sent successfully to {emailMessage.RecipientAddress}");
            }
            catch (JsonException ex)
            {
                // Track JSON deserialization failures
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "messageBody", messageBody },
                    { "errorType", "JsonDeserialization" }
                });
                
                _logger.LogError(ex, "Error deserializing message");
                throw; // This will cause the message to be moved to the dead-letter queue
            }
            catch (Exception ex)
            {
                // Track all other exceptions
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "messageBody", messageBody },
                    { "errorType", "ProcessingError" }
                });
                
                _logger.LogError(ex, "Error processing message");
                throw; // This will trigger the retry policy
            }
        }

        private async Task SendEmailAsync(EmailMessage message)
        {
            using var smtpClient = new SmtpClient
            {
                Host = Environment.GetEnvironmentVariable("SMTP_HOST"),
                Port = int.Parse(Environment.GetEnvironmentVariable("SMTP_PORT")),
                Credentials = new NetworkCredential(
                    Environment.GetEnvironmentVariable("SMTP_USERNAME"),
                    Environment.GetEnvironmentVariable("SMTP_PASSWORD")
                ),
                EnableSsl = true
            };

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(Environment.GetEnvironmentVariable("FROM_ADDRESS")),
                Subject = message.Subject,
                Body = message.Body,
                IsBodyHtml = true
            };
            
            mailMessage.To.Add(message.RecipientAddress);
            
            await smtpClient.SendMailAsync(mailMessage);
        }
    }
} 