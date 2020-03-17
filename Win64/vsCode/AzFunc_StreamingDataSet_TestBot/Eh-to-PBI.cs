using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using System.Net.Http;

using System.Threading.Tasks;

namespace SensorSim
{
    public static class TimerTriggerCSharp
    {
        [FunctionName("TimerTriggerCSharp")]
        public static async void Run([TimerTrigger("*/5 * * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            
            string powerBI_API = "https://api.powerbi.cn/beta/4bdd9bf4-3bc1-411d-bf50-2c33f64cf161/datasets/b583265d-9102-4728-8203-c2ead1ee46f7/rows?key=JoLEXX48q8RQDF%2FI%2FZr8qwc5AD7BsJB%2B546LPDP6bjLCA%2BJcsA2qLU%2BOwKf%2Ft12HVooykHuieaSGNYsq11HrzA%3D%3D";


            /*
            {time:"2020-02-08T08:49:46.817Z", loc:"Shanghai", st_temp:37.2, max_temp:40, min_temp:35, ir_temp:37.3, veri_temp:37.1, ir_id:"2fe6c5b4-7918-46cb-96f4-8c4c5cb2fd31", rgb_id:"2fe6c5b4-7918-46cb-96f4-8c4c5cb2fd32", subject_id:"2fe6c5b4-7918-46cb-96f4-8c4c5cb2fd33", gatekeeper_id:"2fe6c5b4-7918-46cb-96f4-8c4c5cb2fd31"} 

            time 是时间戳，loc是设备地址，st_temp标准温度设定，作为当前设备报警阈值，max_temp传感器最大探测温度, min_temp传感器最小探测温度, ir_temp是红外测温读数，veri_temp是手持设备二次验证读数，ir_id是存在blob storage里的红外图片，rgb_id是RGB图片，subject_id是被检出的高温者身份，gatekeeper_id是门卫的工作人员身份
            */

            //sensor measurement range specified by hardware
            const int temp_min = 30; 
            const int temp_max = 45;

            //using random number to simulate sensor reading, replacing with real number read from sensor in reqeust
            Random random = new Random();  
            int fp = random.Next(1, 9);
            int temp = random.Next(temp_min, temp_min+2);
            string loc = "TestBot";//fp%2==0? "Beijing": "Shanghai";
            var sensorReading = temp+fp*0.1;  

            string payload = string.Format(@"[{{
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

            HttpClient client = new HttpClient();
            HttpContent content = new StringContent(payload, UnicodeEncoding.UTF8,"application/json");
            HttpResponseMessage response = await client.PostAsync(powerBI_API, content);

            response.EnsureSuccessStatusCode();
            
        }
    }
}
