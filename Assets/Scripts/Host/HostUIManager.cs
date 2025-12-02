using System.Globalization;
using System.Linq;
using Core;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Host
{
    public class HostUIManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameSessionManager sessionManager;
        [SerializeField] private HostNetworkManager networkManager;
        
        [Header("UI Panels")]
        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private GameObject promptPanel;
        [SerializeField] private GameObject submitPanel;
        
        [Header("Lobby UI")]
        [SerializeField] private Button startGameButton;
        [SerializeField] private TextMeshProUGUI playerListText;
        [SerializeField] private TextMeshProUGUI roomCodeText;
        
        [Header("Prompt UI")]
        [SerializeField] private TextMeshProUGUI promptText;
        [SerializeField] private TextMeshProUGUI promptTimerText;
        
        [Header("Submit UI")]
        [SerializeField] private TextMeshProUGUI submitStatusText;
        
        private void Awake()
        {
            if (sessionManager == null)
            {
                sessionManager = GetComponent<GameSessionManager>();
            }
            if (networkManager == null)
            {
                networkManager = GetComponent<HostNetworkManager>();
            }
        }
        
        private void OnEnable()
        {
            sessionManager.OnStateChanged += HandleStateChanged;
            sessionManager.OnPlayerJoined += HandlePlayerJoined;
            sessionManager.OnPlayerLeft += HandlePlayerLeft;
        }

        private void OnDisable()
        {
            sessionManager.OnStateChanged -= HandleStateChanged;
            sessionManager.OnPlayerJoined -= HandlePlayerJoined;
            sessionManager.OnPlayerLeft -= HandlePlayerLeft;
        }
        
        private void Start()
        {
            ShowPanel(GameState.Lobby);
            UpdateLobbyUI();
        }
        
        public void OnStartHostClicked()
        {
            networkManager.StartHost();
            if (roomCodeText != null)
            {
                roomCodeText.text = "Room Code: HOST";
            }
        }
        
        public void OnStartGameClicked()
        {
            sessionManager.StartGame();
        }
        
        private void HandleStateChanged(GameState previousState, GameState newState)
        {
            ShowPanel(newState);

            switch (newState)
            {
                case GameState.Lobby:
                    UpdateLobbyUI();
                    break;
                case GameState.Prompt:
                    UpdatePromptUI();
                    break;
                case GameState.Submit:
                    UpdateSubmitUI();
                    break;
            }
        }
        
        private void HandlePlayerJoined(PlayerData player)
        {
            UpdateLobbyUI();
        }

        private void HandlePlayerLeft(string playerId)
        {
            UpdateLobbyUI();
        }

        private void ShowPanel(GameState state)
        {
            lobbyPanel?.SetActive(state == GameState.Lobby);
            promptPanel?.SetActive(state == GameState.Prompt || state == GameState.Submit);
            submitPanel?.SetActive(state == GameState.Submit);
        }
        
        private void UpdateLobbyUI()
        {
            if (!playerListText)
            {
                return;
            }
            
            string playerListString = "Players:\n";
            
            var players = sessionManager.Players.Values.ToList();
            foreach (var player in players)
            {
                playerListString += $"• {player.playerName}\n";
            }
            playerListText.text = playerListString;
            
            startGameButton.interactable = (players.Count() >= sessionManager.MinPlayers);
        }
        
        private void UpdatePromptUI()
        {
            promptText.text = sessionManager.GetCurrentPrompt();
            promptTimerText.text = sessionManager.StateTimer.ToString(CultureInfo.InvariantCulture);
        }
        
        private void UpdateSubmitUI()
        {
            submitStatusText.text = "Waiting for players to\nsubmit answers...";
        }
    }
}
