using System.Collections.Generic;
using System.Linq;
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
            sessionManager.OnAnswerSubmitted += HandleAnswerSubmitted;
            sessionManager.OnVoteSubmitted += HandleVoteSubmitted;
        }
        
        private void OnDisable()
        {
            if (sessionManager != null)
            {
                sessionManager.OnStateChanged -= HandleStateChanged;
                sessionManager.OnPlayerJoined -= HandlePlayerJoined;
                sessionManager.OnPlayerLeft -= HandlePlayerLeft;
                sessionManager.OnAnswerSubmitted -= HandleAnswerSubmitted;
                sessionManager.OnVoteSubmitted -= HandleVoteSubmitted;
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
            else if (_keyboard.aKey.wasPressedThisFrame)
            {
                SubmitAllAnswers();
            }
            else if (_keyboard.vKey.wasPressedThisFrame)
            {
                SubmitAllVotes();
            }
        }
        
        // Test Actions
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
        
        public void SubmitAllAnswers()
        {
            if (sessionManager.CurrentState != GameState.Submit)
            {
                Debug.LogWarning("[Tester] Not in Submit state");
                return;
            }

            string[] sampleAnswers = new[] 
            {
                "strawberry",
                "meatloaf",
                "onion"
            };

            for (int i = 0; i < _testPlayerIds.Count; i++)
            {
                string answer = sampleAnswers[i % sampleAnswers.Length];
                sessionManager.SubmitAnswer(_testPlayerIds[i], answer);
            }

            Debug.Log($"[Tester] Submitted {_testPlayerIds.Count} answers");
        }
        
        public void SubmitAllVotes()
        {
            if (sessionManager.CurrentState != GameState.Vote)
            {
                Debug.LogWarning("[Tester] Not in Vote state");
                return;
            }

            var options = sessionManager.GetVotingOptions();
            
            // Each player votes for a random option (not their own)
            foreach (string playerId in _testPlayerIds)
            {
                var votableOptions = options.Where(o => o.optionId != playerId).ToList();
                if (votableOptions.Count > 0)
                {
                    var randomOption = votableOptions[Random.Range(0, votableOptions.Count)];
                    sessionManager.SubmitVote(playerId, randomOption.optionId);
                }
            }

            Debug.Log($"[Tester] Submitted {_testPlayerIds.Count} votes");
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
        
        private void HandleAnswerSubmitted(string playerId, string answer)
        {
            Debug.Log($"[Tester] Answer Submitted: {answer}");
        }
        
        private void HandleVoteSubmitted(string playerId, string optionId)
        {
            Debug.Log($"[Tester] Vote Submitted by {playerId}");
        }
    }
}
