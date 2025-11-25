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
        
        [Header("Lobby UI")]
        [SerializeField] private TextMeshProUGUI playerListText;
        [SerializeField] private TextMeshProUGUI roomCodeText;
        
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
            sessionManager.OnPlayerJoined += HandlePlayerJoined;
            sessionManager.OnPlayerLeft += HandlePlayerLeft;
        }

        private void OnDisable()
        {
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
        }
        
        private void UpdateLobbyUI()
        {
            if (playerListText != null)
            {
                string playerList = "Players:\n";
                foreach (var player in sessionManager.Players.Values)
                {
                    playerList += $"• {player.playerName}\n";
                }
                playerListText.text = playerList;
            }
        }
    }
}
