using System;
using System.Collections.Generic;
using Core;
using UnityEngine;

namespace Host
{
    public class GameSessionManager : MonoBehaviour
    {
        [Header("Game Configuration")]
        [SerializeField] private int minPlayers = 3;
        [SerializeField] private int maxPlayers = 5;
        [SerializeField] private float promptTimeLimit = 60f;
        [SerializeField] private float submitTimeLimit = 45f;
        
        [Header("Prompts")] [SerializeField] private string[] prompts;
        
        // State
        private GameState _currentState = GameState.Lobby;
        private Dictionary<string, PlayerData> _players;
        private string _currentPrompt;
        private int _currentPromptIndex = 0;
        private float _stateTimer = 0f;
        
        // Events
        public event Action<GameState, GameState> OnStateChanged;
        public event Action<PlayerData> OnPlayerJoined;
        public event Action<string> OnPlayerLeft;

        public GameState CurrentState => _currentState;
        public IReadOnlyDictionary<string, PlayerData> Players => _players;
        public int PlayerCount => _players.Count;

        private void Awake()
        {
            _players = new Dictionary<string, PlayerData>();
            
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
            return _currentState == GameState.Lobby;
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
                    // OnEnterLobby();
                    break;
                case GameState.Prompt:
                    OnEnterPrompt();
                    break;
                case GameState.Submit:
                    OnEnterSubmit();
                    break;
                case GameState.Vote:
                    // OnEnterVote();
                    break;
                case GameState.Reveal:
                    // OnEnterReveal();
                    break;
                case GameState.GameOver:
                    // OnEnterGameOver();
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
            }
        }

        // State Handlers
        private void OnEnterPrompt()
        {
            _currentPrompt = _currentPromptIndex < prompts.Length ? prompts[_currentPromptIndex] : "Default prompt";
            
            _stateTimer = promptTimeLimit;
            Debug.Log($"Prompt: {_currentPrompt}");
        }
        
        private void OnEnterSubmit()
        {
            _stateTimer = submitTimeLimit;
        }
    }
}
