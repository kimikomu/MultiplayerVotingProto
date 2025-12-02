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
        
        private InMemoryTransport _transport;
        
        private string _myPlayerId;
        private string _myPlayerName;
        private float _heartbeatTimer = 0f;
        private const float HEARTBEAT_INTERVAL = 2f;
        
        // Events for UI
        public event Action<Payloads.JoinResponsePayload> OnJoinResponse;
        public event Action<Payloads.PlayerJoinedPayload> OnPlayerJoined;
        public event Action<Payloads.StateChangedPayload> OnStateChanged;
        public event Action<Payloads.PromptPayload> OnPromptReceived;
        
        public string MyPlayerId => _myPlayerId;
        public string MyPlayerName => _myPlayerName;
        
        private void Awake()
        {
            // Always create a new transport instance for this client
            _transport = gameObject.AddComponent<InMemoryTransport>();
            Debug.Log($"[ClientNetworkManager] Created new transport instance {_transport.GetInstanceID()} for client");
        }
        
        private void OnEnable()
        {
            if (_transport != null)
            {
                _transport.OnMessageReceived += HandleMessageReceived;
            }
        }
        
        private void OnDisable()
        {
            if (_transport != null)
            {
                _transport.OnMessageReceived -= HandleMessageReceived;
            }
        }
        
        private void Update()
        {
            if (_transport is null || !_transport.IsConnected || string.IsNullOrEmpty(_myPlayerId)) return;
            
            _heartbeatTimer += Time.deltaTime;
            if (_heartbeatTimer >= HEARTBEAT_INTERVAL)
            {
                SendHeartbeat();
                _heartbeatTimer = 0f;
            }
        }
        
        public void ConnectToServer(string playerName)
        {
            _myPlayerName = playerName;
            
            Debug.Log($"[ClientNetworkManager] {playerName} connecting with transport {_transport.GetInstanceID()}");
            _transport.Connect(serverAddress, serverPort);
            
            // Wait a frame then send join request
            Invoke(nameof(SendJoinRequest), 0.1f);
        }
        
        public void Disconnect()
        {
            if (_transport != null)
            {
                _transport.Disconnect();
            }
            _myPlayerId = null;
        }
        
        private void SendJoinRequest()
        {
            Payloads.JoinRequestPayload payload = new Payloads.JoinRequestPayload
            {
                playerName = _myPlayerName
            };

            SendToServer(MessageTypes.JOIN_REQUEST, JsonUtility.ToJson(payload));
        }
        
        private void SendHeartbeat()
        {
            SendToServer(MessageTypes.HEARTBEAT, "{}");
        }
        
        private void SendToServer(string messageType, string payloadJson)
        {
            NetworkMessage message = new NetworkMessage(messageType, payloadJson, _myPlayerId);
            _transport.SendToServer(message.ToJson());
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
                Debug.LogError($"[CLIENT] Failed to parse message: {e.Message}");
            }
        }
        
        private void HandleJoinResponse(NetworkMessage message)
        {
            Payloads.JoinResponsePayload payload = JsonUtility.FromJson<Payloads.JoinResponsePayload>(message.payload);
            
            if (payload.success)
            {
                _myPlayerId = payload.playerId;
                Debug.Log($"[CLIENT] Successfully joined as {payload.playerName} ({_myPlayerId})");
            }
            else
            {
                Debug.LogError($"[CLIENT] Failed to join: {payload.reason}");
            }

            OnJoinResponse?.Invoke(payload);
        }
        
        private void HandlePlayerJoined(NetworkMessage message)
        {
            Payloads.PlayerJoinedPayload payload = JsonUtility.FromJson<Payloads.PlayerJoinedPayload>(message.payload);
            OnPlayerJoined?.Invoke(payload);
        }
        
        private void HandleStateChanged(NetworkMessage message)
        {
            Payloads.StateChangedPayload payload = JsonUtility.FromJson<Payloads.StateChangedPayload>(message.payload);
            OnStateChanged?.Invoke(payload);
        }

        private void HandlePromptSent(NetworkMessage message)
        {
            Payloads.PromptPayload payload = JsonUtility.FromJson<Payloads.PromptPayload>(message.payload);
            OnPromptReceived?.Invoke(payload);
        }
        
        private void ProcessMessage(NetworkMessage message)
        {
            switch (message.type)
            {
                case MessageTypes.JOIN_RESPONSE:
                    HandleJoinResponse(message);
                    break;
                case MessageTypes.PLAYER_JOINED:
                    // HandlePlayerJoined(message);
                    Debug.Log($"[CLIENT] PLAYER_JOINED message type");
                    break;
                case MessageTypes.STATE_CHANGED:
                    HandleStateChanged(message);
                    break;
                case MessageTypes.PROMPT_SENT:
                    HandlePromptSent(message);
                    break;
                default:
                    // Ignore unknown message types;
                    break;
            }
        }
        
        private void OnDestroy()
        {
            if (_transport != null && _transport.IsConnected)
            {
                _transport.Disconnect();
            }
        }
    }
}
