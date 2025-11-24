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
        private bool _isServer = false;
        private bool _isClient = false;
        private bool _isConnected = false;
        private string _clientId = "";
        
        private Queue<(string senderId, string message)> _serverMessageQueue;
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
            Debug.Log($"[InMemoryTransport] Server started on port {port}");
        }

        public override void StopServer()
        {
            _isServer = false;
            _isConnected = false;
            _serverMessageQueue.Clear();
            Debug.Log("[InMemoryTransport] Server stopped");
        }

        public override void Connect(string address, int port)
        {
            _isClient = true;
            _isConnected = true;
            _clientId = Guid.NewGuid().ToString();
            Debug.Log($"[InMemoryTransport] Client connected: {_clientId}");
            
            // Notify server (simulate)
            if (_isServer)
            {
                InvokeClientConnected(_clientId);
            }
        }

        public override void Disconnect()
        {
            if (_isClient && _isConnected)
            {
                Debug.Log($"[InMemoryTransport] Client disconnected: {_clientId}");
                if (_isServer)
                {
                    InvokeClientDisconnected(_clientId);
                }
                _isClient = false;
                _isConnected = false;
                _clientMessageQueue.Clear();
            }
        }

        public override void SendToClient(string targetClientId, string message)
        {
            if (!_isServer) return;
            // In real implementation, route to a specific client
            // For this in-memory version with a single client, just enqueue
            if (targetClientId == _clientId)
            {
                _clientMessageQueue.Enqueue(message);
            }
        }

        public override void SendToServer(string message)
        {
            if (!_isClient || !_isConnected) return;
            _serverMessageQueue.Enqueue((_clientId, message));
        }

        public override void SendToAllClients(string message)
        {
            if (!_isServer) return;
            _clientMessageQueue.Enqueue(message);
        }
    }
}
