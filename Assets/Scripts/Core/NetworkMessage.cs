using System;
using UnityEngine;

namespace Core
{
    [Serializable]
    public class NetworkMessage
    {
        public string type;
        public string payload;
        public string senderId;
        public long timestamp;
        
        public NetworkMessage(string messageType, string jsonPayload, string sender = "")
        {
            type = messageType;
            payload = jsonPayload;
            senderId = sender;
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }
        
        public static NetworkMessage FromJson(string json)
        {
            return JsonUtility.FromJson<NetworkMessage>(json);
        }
    }
}
