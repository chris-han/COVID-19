// Copyright (c) Microsoft. All rights reserved.
namespace EdgeModule_Simulated_IR_Cam
{
    using System;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using Microsoft.Azure.Devices.Edge.Util;
    using Microsoft.Azure.Devices.Edge.Util.Concurrency;
    using Microsoft.Azure.Devices.Edge.Util.TransientFaultHandling;
    using Microsoft.Azure.Devices.Shared;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;
    using ExponentialBackoff = Microsoft.Azure.Devices.Edge.Util.TransientFaultHandling.ExponentialBackoff;

    class Program
    {
        static int Sensor_Temperature_Min { get; set; } = 30;
        static int Sensor_Temperature_Max { get; set; } = 45;
        static int MessageInterval { get; set; } = 8; // TimeSpan.FromSeconds(5);
        static bool ActivateCam { get; set; } = true;
        static string SensorLocation { get; set; } = "Beijing";

        const double Temperature_Threshhold  = 37.2;
        //const string MessageCountConfigKey = "MessageCount";
        //const string SendDataConfigKey = "SendData";
        //const string SendIntervalConfigKey = "SendInterval";

        static readonly ITransientErrorDetectionStrategy DefaultTimeoutErrorDetectionStrategy =
            new DelegateErrorDetectionStrategy(ex => ex.HasTimeoutException());

        static readonly RetryStrategy DefaultTransientRetryStrategy =
            new ExponentialBackoff(
                5,
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(60),
                TimeSpan.FromSeconds(4));

        static readonly Guid BatchId = Guid.NewGuid();
        static readonly AtomicBoolean Reset = new AtomicBoolean(false);
        static readonly Random Rnd = new Random();


        public enum ControlCommandEnum
        {
            Reset = 0,
            NoOperation = 1
        }

        public static int Main() => MainAsync().Result;

        static async Task<int> MainAsync()
        {
            Console.WriteLine("IR Cam Simulator Main() started.");

            Console.WriteLine(
                $"Initializing simulated temperature sensor to send "
                + $"messages, at an interval of {MessageInterval} seconds.\n"
                + $"To change this, set the device twin variable 'MessageInterval' to the number of messages that should be sent (set it to -1 to send unlimited messages).");

            TransportType transportType = TransportType.Amqp_Tcp_Only;

            ModuleClient moduleClient = await CreateModuleClientAsync(
                transportType,
                DefaultTimeoutErrorDetectionStrategy,
                DefaultTransientRetryStrategy);
            await moduleClient.OpenAsync();
            await moduleClient.SetMethodHandlerAsync("reset", ResetMethod, null);

            (CancellationTokenSource cts, ManualResetEventSlim completed, Option<object> handler) = ShutdownHandler.Init(TimeSpan.FromSeconds(5), null);

            Twin currentTwinProperties = await moduleClient.GetTwinAsync();
            if (currentTwinProperties.Properties.Desired.Contains("MessageInterval"))
            {
                MessageInterval = currentTwinProperties.Properties.Desired["MessageInterval"];// TimeSpan.FromSeconds((int)currentTwinProperties.Properties.Desired["MessageInterval"]);
            }

            if (currentTwinProperties.Properties.Desired.Contains("ActivateCam"))
            {
                ActivateCam = (bool)currentTwinProperties.Properties.Desired["ActivateCam"];
                if (!ActivateCam)
                {
                    Console.WriteLine("Sending data disabled. Change twin configuration to start sending again.");
                }
            }

            ModuleClient userContext = moduleClient;
            await moduleClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertiesUpdated, userContext);
            await moduleClient.SetInputMessageHandlerAsync("control", ControlMessageHandle, userContext);
            await SendEvents(moduleClient, cts);
            await cts.Token.WhenCanceled();

            completed.Set();
            handler.ForEach(h => GC.KeepAlive(h));
            Console.WriteLine("IR Cam Simulator Main() finished.");
            return 0;
        }

        //static bool SendUnlimitedMessages(int maximumNumberOfMessages) => maximumNumberOfMessages < 0;

        // Control Message expected to be:
        // {
        //     "command" : "reset"
        // }
        static Task<MessageResponse> ControlMessageHandle(Message message, object userContext)
        {
            byte[] messageBytes = message.GetBytes();
            string messageString = Encoding.UTF8.GetString(messageBytes);

            Console.WriteLine($"Received message Body: [{messageString}]");

            try
            {
                var messages = JsonConvert.DeserializeObject<ControlCommand[]>(messageString);

                foreach (ControlCommand messageBody in messages)
                {
                    if (messageBody.Command == ControlCommandEnum.Reset)
                    {
                        Console.WriteLine("Resetting temperature sensor..");
                        Reset.Set(true);
                    }
                }
            }
            catch (JsonSerializationException)
            {
                var messageBody = JsonConvert.DeserializeObject<ControlCommand>(messageString);

                if (messageBody.Command == ControlCommandEnum.Reset)
                {
                    Console.WriteLine("Resetting temperature sensor..");
                    Reset.Set(true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Failed to deserialize control command with exception: [{ex}]");
            }

            return Task.FromResult(MessageResponse.Completed);
        }

        static Task<MethodResponse> ResetMethod(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine("Received direct method call to reset temperature sensor...");
            Reset.Set(true);
            var response = new MethodResponse((int)HttpStatusCode.OK);
            return Task.FromResult(response);
        }

        /// <summary>
        /// Module behavior:
        ///        Sends data periodically (with default frequency of 5 seconds).
        ///        Data trend:
        ///         - Machine Temperature regularly rises from 21C to 100C in regularly with jitter
        ///         - Machine Pressure correlates with Temperature 1 to 10psi
        ///         - Ambient temperature stable around 21C
        ///         - Humidity is stable with tiny jitter around 25%
        ///                Method for resetting the data stream.
        /// </summary>
        static async Task SendEvents(
            ModuleClient moduleClient,
            //SimulatorParameters sim,
            CancellationTokenSource cts)
        {
            int count = 1;
            double currentTemp = Sensor_Temperature_Min;
            
            while (!cts.Token.IsCancellationRequested)
            {
                if (Reset)
                {
                    currentTemp = Sensor_Temperature_Min;
                count = 1;
                    Reset.Set(false);
                }

                currentTemp = Rnd.Next(Sensor_Temperature_Min, Sensor_Temperature_Max-1)+Rnd.NextDouble();
                
                if (ActivateCam)
                {
                    var messageData = new MessageBody
                    {
                        SensorTime = DateTime.UtcNow.ToLocalTime(),

                        SensorLocation = SensorLocation,

                        Temperature_Threshhold = Temperature_Threshhold,

                        Sensor_Temperature_Min = Sensor_Temperature_Min,

                        Sensor_Temperature_Max = Sensor_Temperature_Max,

                        Sensor_Temperature_Reading= currentTemp,

                        Subject_Temperature_Verification = Temperature_Threshhold - Rnd.NextDouble(),

                        IR_Picture_Id = "IR_Picture1.jpg",
                        RGB_Picture_Id = " RGB_Picture1.jpg",

                        Subject_Id ="person1",
                        Gatekeeper_Id ="Security Guard 007"

                    };

                    string dataBuffer = JsonConvert.SerializeObject(messageData);
                    var eventMessage = new Message(Encoding.UTF8.GetBytes(dataBuffer));
                    eventMessage.Properties.Add("sequenceNumber", count.ToString());
                    eventMessage.Properties.Add("batchId", BatchId.ToString());
                    Console.WriteLine($"\t{DateTime.Now.ToLocalTime()}> Sending message: {count}, Body: [{dataBuffer}]");

                    /*******************************************************
                    * ****************            **************************
                    ***************** SENDING DATA *************************
                    * ****************            **************************
                    * ******************************************************/
                    await moduleClient.SendEventAsync("output", eventMessage);
                    Console.WriteLine($"\t{DateTime.Now.ToLocalTime()}> Message {count}: Sent Seuccessfully!");
                    count++;
                }

                TimeSpan pauseTime = TimeSpan.FromSeconds(MessageInterval);
                await Task.Delay(pauseTime, cts.Token);
            }
        }

        static async Task OnDesiredPropertiesUpdated(TwinCollection desiredPropertiesPatch, object userContext)
        {
            // At this point just update the configure configuration.
            if (desiredPropertiesPatch.Contains("MessageInterval"))
            {
                MessageInterval = (int)desiredPropertiesPatch["MessageInterval"];
            }

            if (desiredPropertiesPatch.Contains("SensorLocation"))
            {
                SensorLocation = (string)desiredPropertiesPatch["SensorLocation"];
            }

            if (desiredPropertiesPatch.Contains("ActivateCam"))
            {
                bool desiredSendDataValue = (bool)desiredPropertiesPatch["ActivateCam"];
                if (desiredSendDataValue != ActivateCam && !desiredSendDataValue)
                {
                    Console.WriteLine("Sending data disabled. Change twin configuration to start sending again.");
                }

                ActivateCam = desiredSendDataValue;
            }


            var moduleClient = (ModuleClient)userContext;
            var patch = new TwinCollection($"{{ \"ActivateCam\":{ActivateCam.ToString().ToLower()}, \"MessageInterval\": {MessageInterval}}}");
            await moduleClient.UpdateReportedPropertiesAsync(patch); // Just report back last desired property.
        }


        static async Task<ModuleClient> CreateModuleClientAsync(
            TransportType transportType,
            ITransientErrorDetectionStrategy transientErrorDetectionStrategy = null,
            RetryStrategy retryStrategy = null)
        {
            var retryPolicy = new RetryPolicy(transientErrorDetectionStrategy, retryStrategy);
            retryPolicy.Retrying += (_, args) => { Console.WriteLine($"[Error] Retry {args.CurrentRetryCount} times to create module client and failed with exception:{Environment.NewLine}{args.LastException}"); };

            ModuleClient client = await retryPolicy.ExecuteAsync(
                async () =>
                {
                    ITransportSettings[] GetTransportSettings()
                    {
                        switch (transportType)
                        {
                            case TransportType.Mqtt:
                            case TransportType.Mqtt_Tcp_Only:
                                return new ITransportSettings[] { new MqttTransportSettings(TransportType.Mqtt_Tcp_Only) };
                            case TransportType.Mqtt_WebSocket_Only:
                                return new ITransportSettings[] { new MqttTransportSettings(TransportType.Mqtt_WebSocket_Only) };
                            case TransportType.Amqp_WebSocket_Only:
                                return new ITransportSettings[] { new AmqpTransportSettings(TransportType.Amqp_WebSocket_Only) };
                            default:
                                return new ITransportSettings[] { new AmqpTransportSettings(TransportType.Amqp_Tcp_Only) };
                        }
                    }

                    ITransportSettings[] settings = GetTransportSettings();
                    Console.WriteLine($"[Information]: Trying to initialize module client using transport type [{transportType}].");
                    ModuleClient moduleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);
                    await moduleClient.OpenAsync();

                    Console.WriteLine($"[Information]: Successfully initialized module client of transport type [{transportType}].");
                    return moduleClient;
                });

            return client;
        }

        class ControlCommand
        {
            [JsonProperty("command")]
            public ControlCommandEnum Command { get; set; }
        }

        //class SimulatorParameters
        //{
        //    public double SensorTempMin { get; set; }

        //    public double SensorTempMax { get; set; }

        //    //public double MachinePressureMin { get; set; }

        //    //public double MachinePressureMax { get; set; }

        //    //public double AmbientTemp { get; set; }

        //    //public int HumidityPercent { get; set; }
        //}
    }
}
