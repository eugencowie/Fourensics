using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

enum LobbyState { Lobby, InGame, Voting, Finished }

class LobbyScene : MonoBehaviour
{
    public static Lobby Lobby { get; set; }

    [SerializeField] Text m_codeLabel = null;
    [SerializeField] InputField m_codeField = null;
    [SerializeField] Text m_playersLabel = null;
    [SerializeField] GameObject m_startPanel = null;
    [SerializeField] GameObject m_joinPanel = null;
    [SerializeField] GameObject m_lobbyPanel = null;
    [SerializeField] GameObject m_waitPanel = null;
    [SerializeField] GameObject m_startButton = null;

    const int m_maxPlayers = 4;

    async void Start()
    {
        // Show please wait screen
        SwitchPanel(m_waitPanel);

        // Load sign-in scene if user has not yet signed in
        if (SignInScene.User == null)
        {
            SceneManager.LoadScene("SignIn");
            return;
        }

        // Show main screen if user is not yet in a lobby
        if (SignInScene.User.Lobby.Value == null)
        {
            StaticClues.Reset();
            StaticInventory.Reset();
            StaticRoom.Reset();
            StaticSlot.Reset();
            StaticSuspects.Reset();
            SwitchPanel(m_startPanel);
            return;
        }
        else
        {
            // Get user's lobby
            Lobby = await Cloud.Fetch<Lobby>("lobbies", SignInScene.User.Lobby.Value);

            // Show main screen if user's lobby is invalid
            if (Lobby.State.Value == null)
            {
                SignInScene.User.Lobby.Value = null;
                StaticClues.Reset();
                StaticInventory.Reset();
                StaticRoom.Reset();
                StaticSlot.Reset();
                StaticSuspects.Reset();
                SwitchPanel(m_startPanel);
                return;
            }

            // Show lobby screen
            m_codeLabel.text = Lobby.Id;
            RegisterOnPlayersChanged();
            RegisterOnLobbyStateChanged();
            SwitchPanel(m_lobbyPanel);
        }
    }

    /// <summary>
    /// Called when the join button in the start panel is pressed.
    /// </summary>
    public void JoinButtonPressed()
    {
        SwitchPanel(m_joinPanel);
    }

    /// <summary>
    /// Called when the submit button in the join panel is pressed.
    /// </summary>
    public async void SubmitButtonPressed()
    {
        if (!string.IsNullOrEmpty(m_codeField.text))
        {
            SwitchPanel(m_waitPanel);

            // Fetch lobby from cloud
            Lobby lobby = await Cloud.Fetch<Lobby>("lobbies", m_codeField.text.ToUpper());

            bool success = CloudManager.JoinLobby(lobby, m_maxPlayers);
            if (!success)
            {
                m_codeField.text = "";
                SwitchPanel(m_joinPanel);
            }
            else
            {
                Lobby = lobby;
                SignInScene.User.Lobby.Value = Lobby.Id;
                m_codeLabel.text = Lobby.Id;
                RegisterOnPlayersChanged();
                RegisterOnLobbyStateChanged();
                m_startButton.SetActive(false);
                SwitchPanel(m_lobbyPanel);
            }
        }
    }

    /// <summary>
    /// Called when the join button in the start panel is pressed.
    /// </summary>
    public void BackButtonPressed()
    {
        SwitchPanel(m_startPanel);
    }

    /// <summary>
    /// Called when the create button in the start panel is pressed.
    /// </summary>
    public async void CreateButtonPressed()
    {
        SwitchPanel(m_waitPanel);

        string code = await CloudManager.CreateLobbyCode();
        if (string.IsNullOrEmpty(code)) SwitchPanel(m_startPanel);
        else
        {
            Lobby lobby = Cloud.Create<Lobby>("lobbies", code);
            lobby.State.Value = (int)LobbyState.Lobby;

            bool joinSuccess = CloudManager.JoinLobby(lobby, m_maxPlayers);
            if (!joinSuccess) SwitchPanel(m_startPanel);
            else
            {
                Lobby = lobby;
                SignInScene.User.Lobby.Value = Lobby.Id;
                m_codeLabel.text = Lobby.Id;
                RegisterOnPlayersChanged();
                RegisterOnLobbyStateChanged();
                m_startButton.SetActive(true);
                SwitchPanel(m_lobbyPanel);
            }
        }
    }

    /// <summary>
    /// Called when the start button in the lobby panel is pressed.
    /// </summary>
    public async void StartButtonPressed()
    {
        SwitchPanel(m_waitPanel);

        await CloudManager.AssignPlayerScenes(m_codeLabel.text);
        StaticInventory.Hints.Clear();
        Lobby.State.Value = (int)LobbyState.InGame;
    }

    /// <summary>
    /// Called when the leave button in the lobby panel is pressed.
    /// </summary>
    public void LeaveButtonPressed()
    {
        SwitchPanel(m_waitPanel);

        DeregisterOnLobbyStateChanged();
        DeregisterOnPlayersChanged();
        CloudManager.LeaveLobby();
        m_codeLabel.text = "_____";
        SwitchPanel(m_startPanel);
    }

    public void CodeFieldChanged(string s)
    {
        m_codeField.text = m_codeField.text.ToUpper();
    }

    void SwitchPanel(GameObject panel)
    {
        // Disable all panels
        foreach (var p in new GameObject[] { m_startPanel, m_joinPanel, m_lobbyPanel, m_waitPanel })
        {
            p.SetActive(false);
        }

        // Enable specified panel
        panel.SetActive(true);
    }

    void RegisterOnPlayersChanged()
    {
        foreach (CloudNode user in Lobby.Users)
            user.ValueChanged += OnPlayersChanged;
    }

    void DeregisterOnPlayersChanged()
    {
        foreach (CloudNode user in Lobby.Users)
            user.ValueChanged -= OnPlayersChanged;
    }

    void OnPlayersChanged(CloudNode user)
    {
        if (user.Value != null)
        {
            m_playersLabel.text = user.Value.ToString().Replace(',', '\n');
        }
    }

    void RegisterOnLobbyStateChanged()
    {
        Lobby.State.ValueChanged += OnLobbyStateChanged;
    }

    void DeregisterOnLobbyStateChanged()
    {
        Lobby.State.ValueChanged -= OnLobbyStateChanged;
    }

    void OnLobbyStateChanged(CloudNode<long> state)
    {
        if (state.Value.HasValue)
        {
            string statusStr = state.Value.Value.ToString();
            int statusNr = -1;
            if (int.TryParse(statusStr, out statusNr))
            {
                LobbyState s = (LobbyState)statusNr;
                if (s == LobbyState.InGame)
                {
                    int scene = (int)(SignInScene.User.Scene.Value ?? 0);
                    if (scene >= 1 && scene <= 4)
                    {
                        DeregisterOnLobbyStateChanged();
                        DeregisterOnPlayersChanged();
                        SceneManager.LoadScene(scene);
                    }
                }
            }
        }
    }
}
