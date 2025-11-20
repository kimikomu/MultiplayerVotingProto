using System.Collections.Generic;
using Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Host
{
    public class GameSessionTester : MonoBehaviour
    {
        [SerializeField] private GameSessionManager sessionManager;
        
        private List<string> _testPlayerIds;
        private Keyboard _keyboard;
        
        private void Awake()
        {
            if (sessionManager == null)
                sessionManager = GetComponent<GameSessionManager>();
            _testPlayerIds = new List<string>();
            _keyboard = Keyboard.current;
        }
        
        private void OnEnable()
        {
            // Subscribe to events for logging
            sessionManager.OnStateChanged += HandleStateChanged;
            sessionManager.OnPlayerJoined += HandlePlayerJoined;
            sessionManager.OnPlayerLeft += HandlePlayerLeft;
        }
        
        private void OnDisable()
        {
            if (sessionManager != null)
            {
                sessionManager.OnStateChanged -= HandleStateChanged;
                sessionManager.OnPlayerJoined -= HandlePlayerJoined;
                sessionManager.OnPlayerLeft -= HandlePlayerLeft;
            }
        }

        private void Update()
        {
            if (_keyboard == null)
                return;
            
            // Keyboard shortcuts for testing
            if (_keyboard.digit1Key.wasPressedThisFrame)
            {
                AddTestPlayer("Alice");
            }
            else if (_keyboard.digit2Key.wasPressedThisFrame)
            {
                AddTestPlayer("Bob");
            }
            else if (_keyboard.digit3Key.wasPressedThisFrame)
            {
                AddTestPlayer("Charlie");
            }
            else if (_keyboard.digit0Key.wasPressedThisFrame)
            {
                RemoveTestPlayer(0);
            }
            else if (_keyboard.sKey.wasPressedThisFrame)
            {
                StartGame();
            }
        }
        
        private void AddTestPlayer(string testPlayerName)
        {
            var result = sessionManager.AddPlayer(testPlayerName);
            if (result.success)
            {
                _testPlayerIds.Add(result.playerId);
                // Debug.Log($"[Tester] Added player: {testPlayerName} (Total: {sessionManager.PlayerCount})");
            }
            else
            {
                Debug.LogWarning($"[Tester] Failed to add player: {result.reason}");
            }
        }
        
        private void RemoveTestPlayer(int index)
        {
            if (_testPlayerIds.Count <= 0)
            {
                return;
            }
            
            string id = _testPlayerIds[index];
            sessionManager.RemovePlayer(id);
            _testPlayerIds.RemoveAt(index);
            
            // Debug.Log($"[Tester] Removed player at index {index} (Total: {sessionManager.PlayerCount})");
        }
        
        public void StartGame()
        {
            if (sessionManager.CanStartGame())
            {
                sessionManager.StartGame();
                Debug.Log("[Tester] Game Started!");
            }
            else
            {
                Debug.LogWarning("[Tester] Cannot start game - need at least 3 players in lobby");
            }
        }
        
        // Event handlers
        private void HandleStateChanged(GameState prev, GameState next)
        {
            Debug.Log($"[Tester] State: {prev} -> {next}");
        }
        
        private void HandlePlayerJoined(PlayerData player)
        {
            Debug.Log($"[Tester] Player Joined: {player.playerName}");
        }
        
        private void HandlePlayerLeft(string playerId)
        {
            Debug.Log($"[Tester] Player Left: {playerId}");
        }
    }
}
