using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using UnityEngine;

// [GameSessionManager] ←→ [HostNetworkManager] ←→ [Network Transport] ←→ [Clients]
// (Game Logic)           (Translation Layer)      (Network Layer)

namespace Host
{
    public class GameSessionManager : MonoBehaviour
    {
        [Header("Game Configuration")]
        [SerializeField] private int minPlayers = 3;
        [SerializeField] private int maxPlayers = 5;
        [SerializeField] private float promptTimeLimit = 60f;
        [SerializeField] private float submitTimeLimit = 45f;
        [SerializeField] private float voteTimeLimit = 30f;
        [SerializeField] private float revealDuration = 10f;
        
        [Header("Prompts")] [SerializeField] private string[] prompts;
        
        // State
        private GameState _currentState = GameState.Lobby;
        private Dictionary<string, PlayerData> _players;
        private Dictionary<string, string> _playerAnswers;
        private Dictionary<string, string> playerVotes = new Dictionary<string, string>();
        private string _currentPrompt;
        private int _currentPromptIndex = 0;
        private float _stateTimer = 0f;
        
        // Events
        public event Action<GameState, GameState> OnStateChanged;
        public event Action<PlayerData> OnPlayerJoined;
        public event Action<string> OnPlayerLeft;
        public event Action<string, string> OnAnswerSubmitted;
        public event Action<string, string> OnVoteSubmitted;

        public GameState CurrentState => _currentState;
        public IReadOnlyDictionary<string, PlayerData> Players => _players;
        public int PlayerCount => _players.Count;

        private void Awake()
        {
            _players = new Dictionary<string, PlayerData>();
            _playerAnswers = new Dictionary<string, string>();
            
            if (prompts == null || prompts.Length == 0)
            {
                prompts = new string[3];
                prompts[0] = "What's the worst kind of ice cream?";
                prompts[1] = "What's the real reason dinosaurs are extinct?";
                prompts[2] = "Why is the duckfoot platypus the best animal that ever lived?";
            }
        }
        
        private void Update()
        {
            if (_currentState != GameState.Lobby)
            {
                _stateTimer -= Time.deltaTime;
                if (_stateTimer <= 0)
                {
                    OnStateTimerExpired();
                }
            }
        }
        
        // Player Management
        public bool CanJoin()
        {
            return _currentState == GameState.Lobby && _players.Count < maxPlayers;
        }
        
        public (bool success, string playerId, string reason) AddPlayer(string playerName)
        {
            if (!CanJoin())
            {
                return (false, "", "Game is full or already started");
            }

            if (string.IsNullOrWhiteSpace(playerName))
            {
                return (false, "", "Invalid player name");
            }

            string playerId = Guid.NewGuid().ToString();
            PlayerData newPlayer = new PlayerData(playerId, playerName);
            _players.Add(newPlayer.playerId, newPlayer);

            Debug.Log($"Player joined: {playerName} ({playerId})");
            OnPlayerJoined?.Invoke(newPlayer);

            return (true, playerId, "");
        }
        
        public void RemovePlayer(string playerId)
        {
            if (_players.ContainsKey(playerId))
            {
                Debug.Log($"Player left: {_players[playerId].playerName}");
                _players.Remove(playerId);
                OnPlayerLeft?.Invoke(playerId);

                // If we're in a game and not enough players, return to the lobby
                if (_currentState != GameState.Lobby && _players.Count < minPlayers)
                {
                    ChangeState(GameState.Lobby);
                }
            }
        }
        
        public void UpdatePlayerHeartbeat(string playerId)
        {
            if (_players.TryGetValue(playerId, out PlayerData player))
            {
                player.UpdateHeartbeat();
            }
        }
        
        
        // State Machine
        public bool CanStartGame()
        {
            return _currentState == GameState.Lobby && _players.Count >= minPlayers;
        }
        
        public void StartGame()
        {
            if (!CanStartGame())
            {
                Debug.LogWarning("Cannot start game: not enough players or not in lobby");
                return;
            }
            
            _currentPromptIndex = 0;
            ChangeState(GameState.Prompt);
        }
        
        private void ChangeState(GameState newState)
        {
            GameState previousState = _currentState;
            _currentState = newState;

            Debug.Log($"State changed: {previousState} -> {newState}");
            OnStateChanged?.Invoke(previousState, newState);

            switch (newState)
            {
                case GameState.Lobby:
                    OnEnterLobby();
                    break;
                case GameState.Prompt:
                    OnEnterPrompt();
                    break;
                case GameState.Submit:
                    OnEnterSubmit();
                    break;
                case GameState.Vote:
                    OnEnterVote();
                    break;
                case GameState.Reveal:
                    OnEnterReveal();
                    break;
                case GameState.GameOver:
                    OnEnterGameOver();
                    break;
            }
        }
        
        private void OnStateTimerExpired()
        {
            switch (_currentState)
            {
                case GameState.Prompt:
                    Debug.Log($"Prompt going to Submit!");
                    ChangeState(GameState.Submit);
                    break;
                case GameState.Submit:
                    Debug.Log($"Submit going to Vote!");
                    ChangeState(GameState.Vote);
                    break;
                case GameState.Vote:
                    Debug.Log($"Vote going to Reveal!");
                    ChangeState(GameState.Reveal);
                    break;
                case GameState.Reveal:
                    // Next round or game over
                    _currentPromptIndex++;
                    if (_currentPromptIndex < prompts.Length)
                    {
                        Debug.Log($"Reveal going to Prompt!");
                        ChangeState(GameState.Prompt);
                    }
                    else
                    {
                        Debug.Log($"Reveal going to GameOver!");
                        ChangeState(GameState.GameOver);
                    }
                    break;
            }
        }

        // State Handlers
        private void OnEnterLobby()
        {
            _playerAnswers.Clear();
            playerVotes.Clear();
            _stateTimer = 0f;
        }
        
        private void OnEnterPrompt()
        {
            _playerAnswers.Clear();
            playerVotes.Clear();
            
            _currentPrompt = _currentPromptIndex < prompts.Length ?
                prompts[_currentPromptIndex] : "Default prompt";
            
            _stateTimer = promptTimeLimit;
            Debug.Log($"Prompt: {_currentPrompt}");
        }
        
        private void OnEnterSubmit()
        {
            _stateTimer = submitTimeLimit;
        }
        
        private void OnEnterVote()
        {
            _stateTimer = voteTimeLimit;
        }

        private void OnEnterReveal()
        {
            _stateTimer = revealDuration;
            CalculateScores();
        }
        
        private void OnEnterGameOver()
        {
            _stateTimer = 0f;
            Debug.Log("Game Over!");
            
            // Display final scores
            var sortedPlayers = _players.Values.OrderByDescending(p => p.score).ToList();
            for (int i = 0; i < sortedPlayers.Count; i++)
            {
                Debug.Log($"{i + 1}. {sortedPlayers[i].playerName}: {sortedPlayers[i].score} points");
            }
        }
        
        
        // Game Logic
        public void SubmitAnswer(string playerId, string answer)
        {
            if (_currentState != GameState.Submit)
            {
                Debug.LogWarning("Not in Submit state");
                return;
            }

            if (!_players.ContainsKey(playerId))
            {
                Debug.LogWarning($"Unknown player: {playerId}");
                return;
            }

            _playerAnswers[playerId] = answer;
            Debug.Log($"Answer from {_players[playerId].playerName}: {answer}");
            OnAnswerSubmitted?.Invoke(playerId, answer);

            // Auto-advance if everyone submitted
            if (_playerAnswers.Count == _players.Count)
            {
                ChangeState(GameState.Vote);
            }
        }
        
        public List<Payloads.VotingOption> GetVotingOptions()
        {
            List<Payloads.VotingOption> options = new List<Payloads.VotingOption>();

            foreach (var kvp in _playerAnswers)
            {
                options.Add(new Payloads.VotingOption
                {
                    optionId = kvp.Key,
                    optionText = kvp.Value,
                    isPlayerAnswer = true
                });
            }

            // Shuffle options
            System.Random rng = new System.Random();
            options = options.OrderBy(x => rng.Next()).ToList();

            return options;
        }
        
        public void SubmitVote(string playerId, string selectedOptionId)
        {
            if (_currentState != GameState.Vote)
            {
                Debug.LogWarning("Not in Vote state");
                return;
            }

            if (!_players.ContainsKey(playerId))
            {
                Debug.LogWarning($"Unknown player: {playerId}");
                return;
            }

            // Players can't vote for their own answer
            if (selectedOptionId == playerId)
            {
                Debug.LogWarning($"Player {playerId} tried to vote for their own answer");
                return;
            }

            playerVotes[playerId] = selectedOptionId;
            Debug.Log($"Vote from {_players[playerId].playerName} for option {selectedOptionId}");
            OnVoteSubmitted?.Invoke(playerId, selectedOptionId);

            // Auto-advance if everyone voted
            if (playerVotes.Count == _players.Count)
            {
                ChangeState(GameState.Reveal);
            }
        }
        
        public List<Payloads.VoteResult> GetVoteResults()
        {
            List<Payloads.VoteResult> results = new List<Payloads.VoteResult>();

            foreach (var answerKvp in _playerAnswers)
            {
                string authorId = answerKvp.Key;
                string answerText = answerKvp.Value;

                var voters = playerVotes
                    .Where(v => v.Value == authorId)
                    .Select(v => v.Key).ToArray();

                results.Add(new Payloads.VoteResult
                {
                    optionId = authorId,
                    optionText = answerText,
                    voteCount = voters.Length,
                    voterIds = voters,
                    authorId = authorId
                });
            }

            return results.OrderByDescending(r => r.voteCount).ToList();
        }
        
        private void CalculateScores()
        {
            var results = GetVoteResults();

            foreach (var result in results)
            {
                // Author gets points for votes received
                if (_players.TryGetValue(result.authorId, out PlayerData author))
                {
                    author.score += result.voteCount * 100;
                }

                // Voters get points for voting
                foreach (string voterId in result.voterIds)
                {
                    if (_players.TryGetValue(voterId, out PlayerData voter))
                    {
                        voter.score += 50;
                    }
                }
            }
        }
    }
}
