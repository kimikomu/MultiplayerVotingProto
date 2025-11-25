using System.Collections.Generic;
using Core;
using Network;
using UnityEngine;

// [GameSessionManager] ←→ [HostNetworkManager] ←→ [Network Transport] ←→ [Clients]
// (Game Logic)           (Translation Layer)      (Network Layer)

namespace Host
{
    public class HostNetworkManager : MonoBehaviour
    {
        [SerializeField] private int serverPort = 7777;
        [SerializeField] private GameSessionManager sessionManager;
        [SerializeField] private InMemoryTransport transport;

        // Maps network client IDs to game player IDs
        private Dictionary<string, string> _clientIdToPlayerId;
        
        // Maps game player IDs back to network client IDs
        private Dictionary<string, string> _playerIdToClient;
        
        private void Awake()
        {
            if (sessionManager == null)
                sessionManager = GetComponent<GameSessionManager>();

            if (transport == null)
                transport = GetComponent<InMemoryTransport>();
            
            _clientIdToPlayerId = new Dictionary<string, string>();
            _playerIdToClient = new Dictionary<string, string>();
        }

        private void OnEnable()
        {
            transport.OnClientConnected += HandleClientConnected;
            transport.OnClientDisconnected += HandleClientDisconnected;
            transport.OnMessageReceived += HandleMessageReceived;
            
            sessionManager.OnPlayerJoined += HandlePlayerJoined;
            sessionManager.OnPlayerLeft += HandlePlayerLeft;
        } 
        
        private void OnDisable()
        {
            transport.OnClientConnected -= HandleClientConnected;
            transport.OnClientDisconnected -= HandleClientDisconnected;
            transport.OnMessageReceived -= HandleMessageReceived;
            
            sessionManager.OnPlayerJoined -= HandlePlayerJoined;
            sessionManager.OnPlayerLeft -= HandlePlayerLeft;
        }
        
        public void StartHost()
        {
            transport.StartServer(serverPort);
            Debug.Log($"Host started on port {serverPort}");
        }
        
        // Network Event Handlers
        private void HandleClientConnected(string clientId)
        {
            Debug.Log($"Client connected: {clientId}");
        }

        private void HandleClientDisconnected(string clientId)
        {
            Debug.Log($"Client disconnected: {clientId}");
            
            if (_clientIdToPlayerId.TryGetValue(clientId, out string playerId))
            {
                sessionManager.RemovePlayer(playerId);
                _clientIdToPlayerId.Remove(clientId);
                _playerIdToClient.Remove(playerId);
            }
        }

        private void HandleMessageReceived(string senderId, string messageJson)
        {
            try
            {
                NetworkMessage message = NetworkMessage.FromJson(messageJson);
                ProcessMessage(senderId, message);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to parse message: {e.Message}");
            }
        }
        
        // Message Processing
        private void ProcessMessage(string clientId, NetworkMessage message)
        {
            switch (message.type)
            {
                case MessageTypes.JOIN_REQUEST:
                    HandleJoinRequest(clientId, message);
                    break;
                default:
                    Debug.LogWarning($"Unknown message type: {message.type}");
                    break;
            }
        }
        
        //Game Event Handlers
        private void HandleJoinRequest(string clientId, NetworkMessage message)
        {
            Payloads.JoinRequestPayload request = JsonUtility.FromJson<Payloads.JoinRequestPayload>(message.payload);
            
            // Try to add player to the session
            var result = sessionManager.AddPlayer(request.playerName);
            
            // Create response
            Payloads.JoinResponsePayload response = new Payloads.JoinResponsePayload
            {
                success = result.success,
                playerId = result.playerId,
                playerName = request.playerName,
                reason = result.reason
            };
            
            // Map client to player if successful
            if (result.success)
            {
                _clientIdToPlayerId[clientId] = result.playerId;
                _playerIdToClient[result.playerId] = clientId;
            }
            
            // Send the response back to the client
            NetworkMessage responseMsg = new NetworkMessage(MessageTypes.JOIN_RESPONSE, JsonUtility.ToJson(response), "host");
            transport.SendToClient(clientId, responseMsg.ToJson());
            
            Debug.Log($"Join request from {request.playerName}: {(result.success ? "SUCCESS" : result.reason)}");
        }
        
        private void HandlePlayerJoined(PlayerData player)
        {
            Payloads.PlayerJoinedPayload payload = new Payloads.PlayerJoinedPayload
            {
                playerId = player.playerId,
                playerName = player.playerName
            };

            BroadcastToAll(MessageTypes.PLAYER_JOINED, JsonUtility.ToJson(payload));
        }
        
        private void HandlePlayerLeft(string playerId)
        {
            // Broadcast player-left message
            string payload = JsonUtility.ToJson(new { playerId });
            BroadcastToAll(MessageTypes.PLAYER_LEFT, payload);
        }
        
        // Sending Messages
        private void BroadcastToAll(string messageType, string payloadJson)
        {
            NetworkMessage message = new NetworkMessage(messageType, payloadJson, "host");
            transport.SendToAllClients(message.ToJson());
        }
    }
}
