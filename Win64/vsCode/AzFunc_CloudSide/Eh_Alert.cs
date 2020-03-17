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

namespace Eventhub2Pbi
{
    public static class Eh_Alert
    {
        [FunctionName("Eh_Alert")]
        public static async Task Run([EventHubTrigger("eh-steaming-data", Connection = "eh-east2_iothubroutes_workplace-safety-east2_EVENTHUB")] EventData[] events, ILogger log, ExecutionContext context)
        {
            var exceptions = new List<Exception>();

            var config = new ConfigurationBuilder()
                        .SetBasePath(context.FunctionAppDirectory)
                        .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                        .AddEnvironmentVariables()
                        .Build();
            string powerBI_API = config["PBI_API_Alert"];

            foreach (EventData eventData in events)
            {
                try
                {
                    string messageBody = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);

                    // Replace these two lines with your processing logic.
                    log.LogInformation($"C# Event Hub trigger function processed a message: {messageBody}");

                    //string powerBI_API = "https://api.powerbi.com/beta/72f988bf-86f1-41af-91ab-2d7cd011db47/datasets/ceeae206-b5d1-4f63-bc40-e9edfa4ce6b7/rows?key=Lzbmwynjih792VG3ZKAXEKGU06cimyZwp3MqmwOpwGl8sv26scZWiodpnDvTZI2nYSlvru7bJoh7yyTImnDFgQ%3D%3D";
                    //string powerBI_API = "https://api.powerbi.cn/beta/4bdd9bf4-3bc1-411d-bf50-2c33f64cf161/datasets/b304c9ec-4f23-42f8-ae11-ecf443a2f1c3/rows?key=O9WXZn2f8LOGzfXpnyyCBmA0FD1NoO%2BmqlN8oq713AZgzs3CEJfANLiHOa7DhXuLKAxo4zE%2FBCJHLyY%2FPMoTYQ%3D%3D";
                    string payload = string.Format(@"[{0}]", messageBody); //added square brackets for PowerBI stream dataset API to avoid 400 bad request error




                    /*
                    //sensor measurement range specified by hardware
                    const int temp_min = 30; 
                    const int temp_max = 45;

                    //using random number to simulate sensor reading, replacing with real number read from sensor in reqeust
                    Random random = new Random();  
                    int fp = random.Next(1, 9);
                    int temp = random.Next(temp_min, temp_min+2);
                    string loc = "TestBot";//fp%2==0? "Beijing": "Shanghai";
                    var sensorReading = temp+fp*0.1;  

                    payload = string.Format(@"[{{
                        ""time"":""{0}"",
                        ""st_temp"":{1},
                        ""min_temp"":{2},
                        ""max_temp"":{3},
                        ""ir_temp"":{4},
                        ""veri_temp"":{5},                
                        ""ir_id"":""2fe6c5b4-7918-46cb-96f4-8c4c5cb2fd31"",
                        ""rgb_id"":""2fe6c5b4-7918-46cb-96f4-8c4c5cb2fd32"",
                        ""subject_id"":""2fe6c5b4-7918-46cb-96f4-8c4c5cb2fd33"",
                        ""gatekeeper_id"":""2fe6c5b4-7918-46cb-96f4-8c4c5cb2fd34"",
                        ""loc"":""{6}""
                        }}]", DateTime.UtcNow,37.2,temp_min,temp_max,sensorReading,37.1,loc);
                    */





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
