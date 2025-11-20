using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Host
{
    public class GameSessionTester : MonoBehaviour
    {
        [SerializeField] private GameSessionManager sessionManager;
        
        private List<string> testPlayerIds;
        private Keyboard keyboard;
        
        private void Awake()
        {
            if (sessionManager == null)
                sessionManager = GetComponent<GameSessionManager>();
            testPlayerIds = new List<string>();
            keyboard = Keyboard.current;
        }
        
        private void OnEnable()
        {
            // Subscribe to events for logging
            //sessionManager.OnStateChanged += (prev, next) => 
                //Debug.Log($"[Tester] State: {prev} -> {next}");
            
            sessionManager.OnPlayerJoined += (player) => 
                Debug.Log($"[Tester] Player Joined: {player.playerName}");
        }

        private void Update()
        {
            if (keyboard == null)
                return;
            
            // Keyboard shortcuts for testing
            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                AddTestPlayer("Alice");
            }
            else if (keyboard.digit2Key.wasPressedThisFrame)
            {
                AddTestPlayer("Bob");
            }
            else if (keyboard.digit3Key.wasPressedThisFrame)
            {
                AddTestPlayer("Charlie");
            }
        }
        
        private void AddTestPlayer(string testPlayerName)
        {
            var result = sessionManager.AddPlayer(testPlayerName);
            if (result.success)
            {
                testPlayerIds.Add(result.playerId);
                Debug.Log($"[Tester] Added player: {testPlayerName} (Total: {sessionManager.PlayerCount})");
            }
            else
            {
                Debug.LogWarning($"[Tester] Failed to add player: {result.reason}");
            }
        }
    }
}
