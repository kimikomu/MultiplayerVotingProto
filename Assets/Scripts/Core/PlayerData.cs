using System;

namespace Core
{
    [Serializable]
    public class PlayerData
    {
        public string playerId;
        public string playerName;
        public bool isConnected;
        public int score;
        public long lastHeartbeat;
        
        public PlayerData(string id, string name)
        {
            playerId = id;
            playerName = name;
            isConnected = true;
            score = 0;
            lastHeartbeat = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
        
        public void UpdateHeartbeat()
        {
            lastHeartbeat = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}
