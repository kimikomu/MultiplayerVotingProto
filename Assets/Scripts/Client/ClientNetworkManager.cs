using System;
using Core;
using Network;
using UnityEngine;

namespace Client
{
    public class ClientNetworkManager : MonoBehaviour
    {
        [SerializeField] private string serverAddress = "localhost";
        [SerializeField] private int serverPort = 7777;
        [SerializeField] private InMemoryTransport transport;
        
        private string _myPlayerId;
        private string _myPlayerName;
        
        // Events for UI
        public event Action<Payloads.JoinResponsePayload> OnJoinResponse;
        public event Action<Payloads.PlayerJoinedPayload> OnPlayerJoined;
        
        private void Awake()
        {
            if (transport == null)
            {
                transport = GetComponent<InMemoryTransport>();
            }
        }
        
        private void OnEnable()
        {
            if (transport != null)
            {
                transport.OnMessageReceived += HandleMessageReceived;
            }
        }
        
        private void OnDisable()
        {
            if (transport != null)
            {
                transport.OnMessageReceived -= HandleMessageReceived;
            }
        }
        
        public void ConnectToServer(string playerName)
        {
            _myPlayerName = playerName;
            transport.Connect(serverAddress, serverPort);
            
            // Wait a frame then send join request
            Invoke(nameof(SendJoinRequest), 0.1f);
        }
        
        private void SendJoinRequest()
        {
            Payloads.JoinRequestPayload payload = new Payloads.JoinRequestPayload
            {
                playerName = _myPlayerName
            };

            SendToServer(MessageTypes.JOIN_REQUEST, JsonUtility.ToJson(payload));
        }
        
        private void SendToServer(string messageType, string payloadJson)
        {
            NetworkMessage message = new NetworkMessage(messageType, payloadJson, _myPlayerId);
            transport.SendToServer(message.ToJson());
        }
        
        private void HandleMessageReceived(string senderId, string messageJson)
        {
            try
            {
                NetworkMessage message = NetworkMessage.FromJson(messageJson);
                ProcessMessage(message);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse message: {e.Message}");
            }
        }
        
        private void HandleJoinResponse(NetworkMessage message)
        {
            Payloads.JoinResponsePayload payload = JsonUtility.FromJson<Payloads.JoinResponsePayload>(message.payload);
            
            if (payload.success)
            {
                _myPlayerId = payload.playerId;
                Debug.Log($"Successfully joined as {payload.playerName} ({_myPlayerId})");
            }
            else
            {
                Debug.LogError($"Failed to join: {payload.reason}");
            }

            OnJoinResponse?.Invoke(payload);
        }
        
        private void HandlePlayerJoined(NetworkMessage message)
        {
            Payloads.PlayerJoinedPayload payload = JsonUtility.FromJson<Payloads.PlayerJoinedPayload>(message.payload);
            OnPlayerJoined?.Invoke(payload);
        }
        
        private void ProcessMessage(NetworkMessage message)
        {
            switch (message.type)
            {
                case MessageTypes.JOIN_RESPONSE:
                    HandleJoinResponse(message);
                    break;
                case MessageTypes.PLAYER_JOINED:
                    HandlePlayerJoined(message);
                    break;
            }
        }
                
        private void OnDestroy()
        {
            if (transport != null && transport.IsConnected)
            {
                transport.Disconnect();
            }
        }
    }
}
