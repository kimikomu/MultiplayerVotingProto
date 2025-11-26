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
        [SerializeField] private bool useNetworkedClients = false;
        
        [Tooltip( "Assign a prefab with ClientNetworkManager + ClientUIManager" )]
        [SerializeField] private GameObject clientPrefab;

        private List<string> _testPlayerIds;
        private List<GameObject> _clientInstances;
        private Keyboard _keyboard;
        
        private void Awake()
        {
            if (sessionManager == null)
            {
                sessionManager = GetComponent<GameSessionManager>();
            }
            
            _testPlayerIds = new List<string>();
            _clientInstances = new List<GameObject>();
            _keyboard = Keyboard.current;
        }
        
        private void OnEnable()
        {
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
                AddTestPlayer("Kimiko");
            }
            else if (_keyboard.digit2Key.wasPressedThisFrame)
            {
                AddTestPlayer("Mike");
            }
            else if (_keyboard.digit3Key.wasPressedThisFrame)
            {
                AddTestPlayer("Max");
            }
            else if (_keyboard.digit4Key.wasPressedThisFrame)
            {
                AddTestPlayer("Alexis");
            }
            else if (_keyboard.digit5Key.wasPressedThisFrame)
            {
                AddTestPlayer("Kyle");
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
            if (useNetworkedClients)
            {
                AddNetworkedClient(testPlayerName);
            }
            else
            {
                AddDirectPlayer(testPlayerName);
            }
        }
        
        // Add a player directly to the lobby without spawning a client instance
        private void AddDirectPlayer(string testPlayerName)
        {
            var result = sessionManager.AddPlayer(testPlayerName);
            if (result.success)
            {
                _testPlayerIds.Add(result.playerId);
            }
            else
            {
                Debug.LogWarning($"[Tester] Failed to add player: {result.reason}");
            }
        }
        
        // Spawn a client instance for the player and connect to the server
        private void AddNetworkedClient(string testPlayerName)
        {
            if (clientPrefab == null)
            {
                Debug.LogError("[Tester] Client prefab not assigned!");
                return;
            }
            
            GameObject clientInstance = Instantiate(clientPrefab);
            clientInstance.name = $"Client_{testPlayerName}";
            _clientInstances.Add(clientInstance);
            
            var clientNetwork = clientInstance.GetComponent<Client.ClientNetworkManager>();
            if (clientNetwork != null)
            {
                clientNetwork.ConnectToServer(testPlayerName);  // Connecting to the server will also add the player
                                                                // to the lobby at HandlePlayerJoined
            }
            
            Debug.Log($"[Tester] Spawned networked client: {testPlayerName}");
        }
        
        private void RemoveTestPlayer(int index)
        {
            if (useNetworkedClients && _clientInstances.Count > 0)
            {
                // Remove the client instance from the scene
                GameObject clientToRemove = _clientInstances[index];
                _clientInstances.RemoveAt(index);
                Destroy(clientToRemove);
                Debug.Log($"[Tester] Removed networked client at index {index}");
            }
            
            // Remove the player from the lobby
            if (_testPlayerIds.Count <= 0) return;
            string id = _testPlayerIds[index];
            sessionManager.RemovePlayer(id);
            _testPlayerIds.RemoveAt(index);
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

            string[] sampleAnswers =
            {
                "pain",
                "meatloaf",
                "onion",
                "pickles",
                "broccoli"
            };
            
            for (int i = 0; i < _testPlayerIds.Count; i++)
            {
                string answer = sampleAnswers[i % sampleAnswers.Length];
                sessionManager.SubmitAnswer(_testPlayerIds[i], answer);
            }

            Debug.Log($"[Tester] Submitted {_testPlayerIds.Count.ToString()} answers");
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
            if (useNetworkedClients)
            {
                // Add the player to the lobby
                _testPlayerIds.Add(player.playerId);
                Debug.Log($"[Tester] Player Joined by Network: {player.playerName}. Assigned ID: {player.playerId}");
            }
            else
            {
                Debug.Log($"[Tester] Added Direct Player: {player.playerName}");
            }
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
        
        private void OnDestroy()
        {
            // Clean up any spawned clients
            foreach (var client in _clientInstances)
            {
                if (client != null)
                {
                    Destroy(client);
                }
            }
            _clientInstances.Clear();
        }
    }
}
