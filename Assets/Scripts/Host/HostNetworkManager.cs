using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        private Dictionary<string, string> _clientToPlayerId;
        
        // Maps game player IDs back to network client IDs
        private Dictionary<string, string> _playerIdToClient;
        
        private void Awake()
        {
            if (sessionManager == null)
            {
                sessionManager = GetComponent<GameSessionManager>();
            }

            if (transport == null)
            {
                transport = GetComponent<InMemoryTransport>();
            }
            
            _clientToPlayerId = new Dictionary<string, string>();
            _playerIdToClient = new Dictionary<string, string>();
        }

        private void OnEnable()
        {
            transport.OnClientConnected += HandleClientConnected;
            transport.OnClientDisconnected += HandleClientDisconnected;
            transport.OnMessageReceived += HandleMessageReceived;
            
            sessionManager.OnStateChanged += HandleStateChanged;
            sessionManager.OnPlayerJoined += HandlePlayerJoined;
            sessionManager.OnPlayerLeft += HandlePlayerLeft;
        } 
        
        private void OnDisable()
        {
            transport.OnClientConnected -= HandleClientConnected;
            transport.OnClientDisconnected -= HandleClientDisconnected;
            transport.OnMessageReceived -= HandleMessageReceived;
            
            sessionManager.OnStateChanged -= HandleStateChanged;
            sessionManager.OnPlayerJoined -= HandlePlayerJoined;
            sessionManager.OnPlayerLeft -= HandlePlayerLeft;
        }
        
        public void StartHost()
        {
            transport.StartServer(serverPort);
            Debug.Log($"[HOST] Started on port {serverPort}");
        }
        
        public void StopHost()
        {
            transport.StopServer();
            _clientToPlayerId.Clear();
            _playerIdToClient.Clear();
            Debug.Log("[HOST] Stopped");
        }
        
        // Network Event Handlers
        private void HandleClientConnected(string clientId)
        {
            Debug.Log($"[HOST] Client connected: {clientId}");
        }

        private void HandleClientDisconnected(string clientId)
        {
            Debug.Log($"[HOST] Client disconnected: {clientId}");
            
            if (_clientToPlayerId.TryGetValue(clientId, out string playerId))
            {
                sessionManager.RemovePlayer(playerId);
                _clientToPlayerId.Remove(clientId);
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
            catch (Exception e)
            {
                Debug.LogError($"[HOST] Failed to parse message: {e.Message}");
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
                case MessageTypes.HEARTBEAT:
                    HandleHeartbeat(clientId);
                    break;
                case MessageTypes.SUBMIT_ANSWER:
                    HandleSubmitAnswer(clientId, message);
                    break;
                case MessageTypes.PLAYER_JOINED:
                    // Debug.Log("[HOST] PLAYER_JOINED message received from client. Ignoring.");
                    break;
                case MessageTypes.JOIN_RESPONSE:
                    // Debug.Log("[HOST] JOIN_RESPONSE message received from client. Ignoring.");
                    break;
                case MessageTypes.STATE_CHANGED:
                    // Debug.Log("[HOST] STATE_CHANGED message received from client. Ignoring.");
                    break;
                case MessageTypes.PROMPT_SENT:
                    // Debug.Log("[HOST] PROMPT_SENT message received from client. Ignoring.");
                    break;
                default:
                    Debug.LogWarning($"[HOST] Unknown message type: {message.type}");
                    break;
            }
        }
        
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
                _clientToPlayerId[clientId] = result.playerId;
                _playerIdToClient[result.playerId] = clientId;
            }
            
            // Send the response back to the client
            NetworkMessage responseMsg = new NetworkMessage(MessageTypes.JOIN_RESPONSE, JsonUtility.ToJson(response), "host");
            transport.SendToClient(clientId, responseMsg.ToJson());
            
            Debug.Log($"[HOST] Join request from {request.playerName}: {(result.success ? "SUCCESS" : result.reason)}");
        }
        
        private void HandleHeartbeat(string clientId)
        {
            if (_clientToPlayerId.TryGetValue(clientId, out string playerId))
            {
                sessionManager.UpdatePlayerHeartbeat(playerId);
            }
        }
        
        private void HandleSubmitAnswer(string clientId, NetworkMessage message)
        {
            if (!_clientToPlayerId.TryGetValue(clientId, out string playerId))
                return;

            Payloads.SubmitAnswerPayload payload = JsonUtility.FromJson<Payloads.SubmitAnswerPayload>(message.payload);
            sessionManager.SubmitAnswer(playerId, payload.answerText);
        }
        
        // Game Event Handlers
        private void HandleStateChanged(GameState previousState, GameState newState)
        {
            Payloads.StateChangedPayload payload = new Payloads.StateChangedPayload
            {
                newState = newState.ToString(),
                timeLimit = GetTimeLimit(newState)
            };

            BroadcastToAll(MessageTypes.STATE_CHANGED, JsonUtility.ToJson(payload));

            // Send state-specific data
            switch (newState)
            {
                case GameState.Prompt:
                    SendPrompt();
                    break;
            }
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
        private void SendPrompt()
        {
            Payloads.PromptPayload payload = new Payloads.PromptPayload
            {
                promptText = sessionManager.GetCurrentPrompt(),
                timeLimit = GetTimeLimit(GameState.Submit)
            };

            BroadcastToAll(MessageTypes.PROMPT_SENT, JsonUtility.ToJson(payload));
        }
        
        private void BroadcastToAll(string messageType, string payloadJson)
        {
            NetworkMessage message = new NetworkMessage(messageType, payloadJson, "host");
            transport.SendToAllClients(message.ToJson());
        }
        
        private float GetTimeLimit(GameState state)
        {
            return Enum.IsDefined(typeof(GameState), state) ? sessionManager.StateTimer : 0f;
        }
    }
}
