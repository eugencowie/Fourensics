using Firebase.Database;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

class LobbyScene : MonoBehaviour
{
    public static Lobby Lobby
    {
        get { return m_lobby; }
        private set { SignInScene.User.Lobby.Value = (m_lobby = value).Id; }
    }

    static Lobby m_lobby;

    [SerializeField] Text m_codeLabel = null;
    [SerializeField] InputField m_codeField = null;
    [SerializeField] Text m_playersLabel = null;

    [SerializeField] GameObject m_startPanel = null;
    [SerializeField] GameObject m_joinPanel = null;
    [SerializeField] GameObject m_lobbyPanel = null;
    [SerializeField] GameObject m_waitPanel = null;
    [SerializeField] GameObject m_startButton = null;

    const int m_maxPlayers = 4;

    OnlineManager m_network;

    async void Start()
    {
        // Initialise online manager
        m_network = new OnlineManager();

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

        // Fetch user's lobby from the cloud
        Lobby = await Lobby.Fetch(SignInScene.User.Lobby.Value);

        // Show main screen if user's lobby is invalid
        if (Lobby.State.Value == null)
        {
            Lobby = null;
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
        RegisterOnPlayersChanged(Lobby.Id);
        RegisterOnLobbyStateChanged(Lobby.Id);
        SwitchPanel(m_lobbyPanel);
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
            Lobby lobby = await Lobby.Fetch(m_codeField.text.ToUpper());

            bool success = OnlineManager.JoinLobby(lobby, m_maxPlayers);
            if (!success)
            {
                m_codeField.text = "";
                SwitchPanel(m_joinPanel);
            }
            else
            {
                Lobby = lobby;
                m_codeLabel.text = m_codeField.text.ToUpper();
                RegisterOnPlayersChanged(m_codeLabel.text);
                RegisterOnLobbyStateChanged(m_codeLabel.text);
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

        string code = await m_network.CreateLobbyCode();
        if (string.IsNullOrEmpty(code)) SwitchPanel(m_startPanel);
        else
        {
            Lobby lobby = Lobby.Create(code);
            lobby.State.Value = ((int)LobbyState.Lobby).ToString();

            bool joinSuccess = OnlineManager.JoinLobby(lobby, m_maxPlayers);
            if (!joinSuccess) SwitchPanel(m_startPanel);
            else
            {
                Lobby = lobby;
                m_codeLabel.text = code;
                RegisterOnPlayersChanged(code);
                RegisterOnLobbyStateChanged(code);
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

        //LobbyError error = await Network.CanStartGame(CodeLabel.text, MaxPlayers);
        //if (error != LobbyError.None)
        //{
        //    if (error == LobbyError.TooFewPlayers) StatusLabel.text = "too few players, requires " + MaxPlayers;
        //    else if (error == LobbyError.TooManyPlayers) StatusLabel.text = "too many players, requires " + MaxPlayers;
        //    else StatusLabel.text = "unknown error";
        //    SwitchPanel(LobbyPanel);
        //}
        //else
        //{
        await m_network.AssignPlayerScenes(m_codeLabel.text);
        StaticInventory.Hints.Clear();
        m_network.SetLobbyState(LobbyState.InGame);
        //}
    }

    /// <summary>
    /// Called when the leave button in the lobby panel is pressed.
    /// </summary>
    public void LeaveButtonPressed()
    {
        SwitchPanel(m_waitPanel);

        m_network.LeaveLobby();
        DeregisterOnLobbyStateChanged(m_codeLabel.text);
        DeregisterOnPlayersChanged(m_codeLabel.text);
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

    void RegisterOnPlayersChanged(string lobby)
    {
        string roomPlayersKey = string.Format("lobbies/{0}/players", lobby);
        m_network.RegisterListener(roomPlayersKey, OnPlayersChanged);
    }

    void DeregisterOnPlayersChanged(string lobby)
    {
        string roomPlayersKey = string.Format("lobbies/{0}/players", lobby);
        m_network.DeregisterListener(roomPlayersKey, OnPlayersChanged);
    }

    void OnPlayersChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.Snapshot.Exists)
        {
            m_playersLabel.text = args.Snapshot.Value.ToString().Replace(',', '\n');
        }
    }

    void RegisterOnLobbyStateChanged(string lobby)
    {
        string roomStateKey = string.Format("lobbies/{0}/state", lobby);
        m_network.RegisterListener(roomStateKey, OnLobbyStateChanged);
    }

    void DeregisterOnLobbyStateChanged(string lobby)
    {
        string roomStateKey = string.Format("lobbies/{0}/state", lobby);
        m_network.DeregisterListener(roomStateKey, OnLobbyStateChanged);
    }

    void OnLobbyStateChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.Snapshot.Exists)
        {
            string statusStr = args.Snapshot.Value.ToString();
            int statusNr = -1;
            if (int.TryParse(statusStr, out statusNr))
            {
                LobbyState state = (LobbyState)statusNr;
                if (state == LobbyState.InGame)
                {
                    int scene = m_network.GetPlayerScene();
                    if (scene >= 1 && scene <= 4)
                    {
                        DeregisterOnLobbyStateChanged(m_codeLabel.text);
                        DeregisterOnPlayersChanged(m_codeLabel.text);
                        SceneManager.LoadScene(scene);
                    }
                }
            }
        }
    }
}
