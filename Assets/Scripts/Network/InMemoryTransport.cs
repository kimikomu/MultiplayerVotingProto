using System;
using System.Collections.Generic;
using UnityEngine;

// [GameSessionManager] ←→ [HostNetworkManager] ←→ [Network Transport] ←→ [Clients]
// (Game Logic)           (Translation Layer)      (Network Layer)

namespace Network
{
    // Simple in-memory transport for testing (simulates network with message queue)
    public class InMemoryTransport : SimpleNetworkTransport
    {
        // Singleton server instance for all clients to connect to
        private static InMemoryTransport _serverInstance;
        
        // Server-side: track all connected client transports
        private static Dictionary<string, InMemoryTransport> _connectedClients = new Dictionary<string, InMemoryTransport>();

        private bool _isServer = false;
        private bool _isClient = false;
        private bool _isConnected = false;
        private string _clientId = "";
        
        // Server-side: messages received from clients
        private Queue<(string senderId, string message)> _serverMessageQueue;
        
        // Client-side: messages received from server (each client has its own queue)
        private Queue<string> _clientMessageQueue;

        public override bool IsServer => _isServer;
        public override bool IsClient => _isClient;
        public override bool IsConnected => _isConnected;

        private void Awake()
        {
            _serverMessageQueue = new Queue<(string, string)>();
            _clientMessageQueue = new Queue<string>();
        }

        private void Update()
        {
            // Process queued messages
            if (_isServer)
            {
                while (_serverMessageQueue.Count > 0)
                {
                    var msg = _serverMessageQueue.Dequeue();
                    InvokeMessageReceived(msg.senderId, msg.message);
                }
            }

            if (_isClient)
            {
                while (_clientMessageQueue.Count > 0)
                {
                    var msg = _clientMessageQueue.Dequeue();
                    InvokeMessageReceived("server", msg);
                }
            }
        }

        public override void StartServer(int port)
        {
            _isServer = true;
            _isConnected = true;
              _serverInstance = this;
            
            Debug.Log($"[InMemoryTransport] Server started on port {port}");
        }

        public override void StopServer()
        {
            _isServer = false;
            _isConnected = false;
            _serverMessageQueue.Clear();
            
            if (_serverInstance == this)
            {
                _serverInstance = null;
                _connectedClients.Clear();
            }
            
            Debug.Log("[InMemoryTransport] Server stopped");
        }

        public override void Connect(string address, int port)
        {
            if (_serverInstance == null)
            {
                Debug.LogError("[InMemoryTransport] Cannot connect - no server running");
                return;
            }
            
            _isClient = true;
            _isConnected = true;
            _clientId = Guid.NewGuid().ToString();
            
            // Register this client transport instance with the server
            _connectedClients[_clientId] = this;
            
            Debug.Log($"[InMemoryTransport] Client connected: {_clientId}");
            
            // Notify server (simulate)
            Debug.Log($"[InMemoryTransport] Client connected: {_clientId} (Instance: {GetInstanceID()})");
            
            // Notify server
            _serverInstance.InvokeClientConnected(_clientId);
        }

        public override void Disconnect()
        {
            if (!_isClient || !_isConnected)
                return;
        
            Debug.Log($"[InMemoryTransport] Client disconnected: {_clientId}");
            if (_serverInstance != null)
            {
                _serverInstance.InvokeClientDisconnected(_clientId);
                _connectedClients.Remove(_clientId);
            }
            
            _isClient = false;
            _isConnected = false;
            _clientMessageQueue.Clear();
            _clientId = "";
        }

        public override void SendToClient(string targetClientId, string message)
        {
            if (!_isServer)
            {
                Debug.LogWarning("[InMemoryTransport] Cannot send to client - not a server");
                return;
            }
            
            // Find the target client transport and enqueue the message to its queue
            if (_connectedClients.TryGetValue(targetClientId, out InMemoryTransport clientTransport))
            {
                clientTransport._clientMessageQueue.Enqueue(message);
            }
            else
            {
                Debug.LogWarning($"[InMemoryTransport] Cannot send to client {targetClientId} - not found");
            }
        }

        public override void SendToServer(string message)
        {
            if (!_isClient || !_isConnected)
            {
                Debug.LogWarning("[InMemoryTransport] Cannot send to server - not connected");
                return;
            }
            
            if (_serverInstance != null)
            {
                _serverInstance._serverMessageQueue.Enqueue((_clientId, message));
            }
        }

        public override void SendToAllClients(string message)
        {
            if (!_isServer)
            {
                Debug.LogWarning("[InMemoryTransport] Cannot broadcast - not a server");
                return;
            }
            
            // Enqueue message to all connected client transports
            foreach (var clientTransport in _connectedClients.Values)
            {
                clientTransport._clientMessageQueue.Enqueue(message);
            }
        }
        
        private void OnDestroy()
        {
            if (_isServer)
            {
                StopServer();
            }
            else if (_isClient)
            {
                Disconnect();
            }
        }
    }
}
