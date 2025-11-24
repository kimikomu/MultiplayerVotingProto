using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Core;

namespace Client
{
    public class ClientUIManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ClientNetworkManager networkManager;

        [Header("UI Panels")]
        [SerializeField] private GameObject joinPanel;
        
        [Header("Join UI")]
        [SerializeField] private TMP_InputField playerNameInput;
        [SerializeField] private Button joinButton;
        [SerializeField] private TextMeshProUGUI joinStatusText;
        
        private GameState _currentState = GameState.Lobby;

        private void Awake()
        {
            if (networkManager == null)
                networkManager = GetComponent<ClientNetworkManager>();
        }
        
        private void OnEnable()
        {
            joinButton?.onClick.AddListener(OnJoinClicked);
            networkManager.OnJoinResponse += HandleJoinResponse;
        }
        
        private void OnDisable()
        {
            joinButton?.onClick.RemoveListener(OnJoinClicked);
            networkManager.OnJoinResponse -= HandleJoinResponse;
        }
        
        private void Start()
        {
            ShowPanel(joinPanel);
        }
        
        private void OnJoinClicked()
        {
            string playerName = playerNameInput.text.Trim();
            if (string.IsNullOrEmpty(playerName))
            {
                if (joinStatusText != null)
                    joinStatusText.text = "Please enter a name";
                return;
            }

            networkManager.ConnectToServer(playerName);
            joinButton.interactable = false;
            if (joinStatusText != null)
                joinStatusText.text = "Connecting...";
        }
        
        private void HandleJoinResponse(Payloads.JoinResponsePayload payload)
        {
            if (payload.success)
            {
                Debug.Log($"Welcome, {payload.playerName}!\nWaiting for game to start...");
            }
            else
            {
                joinButton.interactable = true;
                if (joinStatusText != null)
                    joinStatusText.text = $"Failed: {payload.reason}";
            }
        }
        
        private void ShowPanel(GameObject panel)
        {
            joinPanel?.SetActive(panel == joinPanel);
        }
    }
}
