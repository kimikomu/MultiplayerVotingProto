using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Core;

namespace Client
{
    public class ClientUIManager : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject joinPanel;
        [SerializeField] private GameObject waitingPanel;
        [SerializeField] private GameObject promptPanel;
        
        [Header("Waiting UI")]
        [SerializeField] private TextMeshProUGUI waitingText;
        
        [Header("Join UI")]
        [SerializeField] private TMP_InputField playerNameInput;
        [SerializeField] private Button joinButton;
        [SerializeField] private TextMeshProUGUI joinStatusText;
        
        [Header("Prompt UI")]
        [SerializeField] private TextMeshProUGUI promptDisplayText;
        [SerializeField] private TextMeshProUGUI promptTimerText;
        
        private GameState _currentState = GameState.Lobby;
        private ClientNetworkManager _networkManager;

        private void Awake()
        {
            _networkManager = gameObject.GetComponent<ClientNetworkManager>();
            
            if (_networkManager == null)
                _networkManager = GetComponent<ClientNetworkManager>();
        }
        
        private void OnEnable()
        {
            joinButton?.onClick.AddListener(OnJoinClicked);
            
            _networkManager.OnJoinResponse += HandleJoinResponse;
            _networkManager.OnStateChanged += HandleStateChanged;
            _networkManager.OnPromptReceived += HandlePromptReceived;
        }
        
        private void OnDisable()
        {
            joinButton?.onClick.RemoveListener(OnJoinClicked);
            
            _networkManager.OnJoinResponse -= HandleJoinResponse;
            _networkManager.OnStateChanged -= HandleStateChanged;
            _networkManager.OnPromptReceived -= HandlePromptReceived;
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

            _networkManager.ConnectToServer(playerName);
            joinButton.interactable = false;
            if (joinStatusText != null)
                joinStatusText.text = "Connecting...";
        }
        
        private void HandleJoinResponse(Payloads.JoinResponsePayload payload)
        {
            if (payload.success)
            {
                ShowPanel(waitingPanel);
                if (waitingText != null)
                    waitingText.text = $"Welcome, {payload.playerName}!\nWaiting for game to start...";
            }
            else
            {
                joinButton.interactable = true;
                if (joinStatusText != null)
                    joinStatusText.text = $"Failed: {payload.reason}";
            }
        }
        
        private void HandleStateChanged(Payloads.StateChangedPayload payload)
        {
            if (System.Enum.TryParse(payload.newState, out GameState newState))
            {
                _currentState = newState;

                switch (newState)
                {
                    case GameState.Lobby:
                        ShowPanel(waitingPanel);
                        break;
                    case GameState.Prompt:
                        ShowPanel(promptPanel);
                        break;
                }
            }
        }

        private void HandlePromptReceived(Payloads.PromptPayload payload)
        {
            promptDisplayText.text = payload.promptText;
            float timeLimit = payload.timeLimit;
            promptTimerText.text = $"Time Limit: {timeLimit}s";
        }
        
        private void ShowPanel(GameObject panel)
        {
            joinPanel?.SetActive(panel == joinPanel);
            waitingPanel?.SetActive(panel == waitingPanel);
            promptPanel?.SetActive(panel == promptPanel);
        }
    }
}
