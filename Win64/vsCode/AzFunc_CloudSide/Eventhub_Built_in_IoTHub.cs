using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using Microsoft.Extensions.Configuration;

namespace IotHubBuiltinEventhub
{
    public static class Eventhub_Built_in_IoTHub
    {
        [FunctionName("Eventhub_Built_in_IoTHub")]
        public static async Task Run([EventHubTrigger("workplace-safety-east2", Connection = "eh-built-in_workplace-safety-east2_IOTHUB")] EventData[] events, ILogger log, ExecutionContext context)
        {
           var exceptions = new List<Exception>();

            var config = new ConfigurationBuilder()
                        .SetBasePath(context.FunctionAppDirectory)
                        .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                        .AddEnvironmentVariables()
                        .Build();
            string powerBI_API = config["PBI_API_Built-in"];

            foreach (EventData eventData in events)
            {
                try
                {
                    string messageBody = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);

                    // Replace these two lines with your processing logic.
                    log.LogInformation($"C# Event Hub trigger function processed a message: {messageBody}");

                    string payload = string.Format(@"[{0}]", messageBody); //added square brackets for PowerBI stream dataset API to avoid 400 bad request error

                    HttpClient client = new HttpClient();
                    HttpContent content = new StringContent(payload, UnicodeEncoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAsync(powerBI_API, content);
                    response.EnsureSuccessStatusCode();
                    await Task.Yield();                    

                }
                catch (Exception e)
                {
                    // We need to keep processing the rest of the batch - capture this exception and continue.
                    // Also, consider capturing details of the message that failed processing so it can be processed again later.
                    exceptions.Add(e);
                }
            }

            // Once processing of the batch is complete, if any messages in the batch failed processing throw an exception so that there is a record of the failure.

            if (exceptions.Count > 1)
                throw new AggregateException(exceptions);

            if (exceptions.Count == 1)
                throw exceptions.Single();
        }
    }
}
