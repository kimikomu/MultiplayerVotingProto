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

        
        // State
        private GameState _currentState = GameState.Lobby;
        private Dictionary<string, PlayerData> _players;
        
        // Events
        public event Action<PlayerData> OnPlayerJoined;
        public event Action<string> OnPlayerLeft;

        public GameState CurrentState => _currentState;
        public IReadOnlyDictionary<string, PlayerData> Players => _players;
        public int PlayerCount => _players.Count;

        private void Awake()
        {
            _players = new Dictionary<string, PlayerData>();
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
        
        private void ChangeState(GameState newState)
        {
            GameState previousState = _currentState;
            _currentState = newState;

            // Debug.Log($"State changed: {previousState} -> {newState}");
            // OnStateChanged?.Invoke(previousState, newState);

            switch (newState)
            {
                case GameState.Lobby:
                    // OnEnterLobby();
                    break;
                case GameState.Prompt:
                    // OnEnterPrompt();
                    break;
                case GameState.Submit:
                    // OnEnterSubmit();
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
    }
}
