using System;
using UnityEngine;

// [GameSessionManager] ←→ [HostNetworkManager] ←→ [Network Transport] ←→ [Clients]
// (Game Logic)           (Translation Layer)      (Network Layer)

namespace Network
{
    public abstract class SimpleNetworkTransport : MonoBehaviour
    {
        public event Action<string> OnClientConnected;
        public event Action<string> OnClientDisconnected;
        public event Action<string, string> OnMessageReceived;

        public abstract void StartServer(int port);
        public abstract void StopServer();
        public abstract void Connect(string address, int port);
        public abstract void Disconnect();
        public abstract void SendToClient(string clientId, string message);
        public abstract void SendToServer(string message);
        public abstract void SendToAllClients(string message);
        public abstract bool IsServer { get; }
        public abstract bool IsClient { get; }
        public abstract bool IsConnected { get; }

        protected void InvokeClientConnected(string clientId)
        {
            OnClientConnected?.Invoke(clientId);
        }
        
        protected void InvokeClientDisconnected(string clientId)
        {
            OnClientDisconnected?.Invoke(clientId);
        }
        
        protected void InvokeMessageReceived(string senderId, string message)
        {
            OnMessageReceived?.Invoke(senderId, message);
        }
    }
}
