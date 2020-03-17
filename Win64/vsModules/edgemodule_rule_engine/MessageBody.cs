// Copyright (c) Microsoft. All rights reserved.
namespace EdgeModule_Simulated_IR_Cam
{
    using System;
    using Newtonsoft.Json;

    // NOTE: IF CHANGING ANYTHING IN THIS FILE, UPDATE MESSAGEBODY.CS IN TEMPERATURE FILTER
    // TODO Put message body in a common lib

    /*
    {time:"2020-02-08T08:49:46.817Z", 
    loc:"Shanghai", 
    st_temp:37.2, 
    max_temp:40, 
    min_temp:35, 
    ir_temp:37.3, 
    veri_temp:37.1, 
    ir_id:"2fe6c5b4-7918-46cb-96f4-8c4c5cb2fd31", 
    rgb_id:"2fe6c5b4-7918-46cb-96f4-8c4c5cb2fd32", 
    subject_id:"2fe6c5b4-7918-46cb-96f4-8c4c5cb2fd33", 
    gatekeeper_id:"2fe6c5b4-7918-46cb-96f4-8c4c5cb2fd31"}
    
    time 是时间戳，loc是设备地址，st_temp标准温度设定，作为当前设备报警阈值，max_temp传感器最大探测温度, min_temp传感器最小探测温度, ir_temp是红外测温读数，
    veri_temp是手持设备二次验证读数，ir_id是存在blob storage里的红外图片，rgb_id是RGB图片，subject_id是被检出的高温者身份，gatekeeper_id是门卫的工作人员身份
    */

    class MessageBody
    {
        [JsonProperty(PropertyName = "time")]
        public DateTime SensorTime { get; set; }

        [JsonProperty(PropertyName = "loc")]
        public string SensorLocation { get; set; }

        [JsonProperty(PropertyName = "st_temp")]
        public double Temperature_Threshhold { get; set; }

        [JsonProperty(PropertyName = "min_temp")]
        public double Sensor_Temperature_Min { get; set; }

        [JsonProperty(PropertyName = "max_temp")]
        public double Sensor_Temperature_Max { get; set; }

        [JsonProperty(PropertyName = "ir_temp")]
        public double Sensor_Temperature_Reading { get; set; }

        [JsonProperty(PropertyName = "veri_temp")]
        public double Subject_Temperature_Verification { get; set; }

        [JsonProperty(PropertyName = "ir_id")]
        public string IR_Picture_Id { get; set; }

        [JsonProperty(PropertyName = "rgb_id")]
        public string RGB_Picture_Id { get; set; }

        [JsonProperty(PropertyName = "subject_id")]
        public string Subject_Id { get; set; }

        [JsonProperty(PropertyName = "gatekeeper_id")]
        public string Gatekeeper_Id { get; set; }

    }
}
