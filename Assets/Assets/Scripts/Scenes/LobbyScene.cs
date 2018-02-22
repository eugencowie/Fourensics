using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

class LobbyScene : MonoBehaviour
{
    [SerializeField] Text m_codeLabel = null;
    [SerializeField] InputField m_codeField = null;

    [SerializeField] GameObject m_startPanel = null;
    [SerializeField] GameObject m_joinPanel = null;
    [SerializeField] GameObject m_lobbyPanel = null;
    [SerializeField] GameObject m_waitPanel = null;

    [SerializeField] GameObject m_startButton = null;

    const int m_maxPlayers = 4;

    async void Start()
    {
        // Show wait panel
        SwitchPanel(m_waitPanel);

        // Get database objects
        User user = await User.Get();
        Lobby lobby = await Lobby.Get(user);

        if (lobby == null || !lobby.State.Value.HasValue)
        {
            // Reset static data
            StaticClues.Reset();
            StaticInventory.Reset();
            StaticRoom.Reset();
            StaticSlot.Reset();
            StaticSuspects.Reset();

            // Show start panel
            SwitchPanel(m_startPanel);
        }
        else
        {
            // Register lobby state change callback
            if (lobby.Users[0].UserId.Value != user.Id)
                lobby.State.ValueChanged += LobbyStateChanged;

            // Show lobby panel
            m_codeLabel.text = lobby.Id;
            SwitchPanel(m_lobbyPanel);
        }
    }

    /// <summary>
    /// Called when the join button in the start panel is pressed.
    /// </summary>
    public void JoinButtonPressed()
    {
        // Show join panel
        SwitchPanel(m_joinPanel);
    }

    /// <summary>
    /// Called when the join button in the start panel is pressed.
    /// </summary>
    public void BackButtonPressed()
    {
        // Show start panel
        SwitchPanel(m_startPanel);
    }

    /// <summary>
    /// Called when the submit button in the join panel is pressed.
    /// </summary>
    public async void SubmitButtonPressed()
    {
        if (!string.IsNullOrEmpty(m_codeField.text))
        {
            // Show wait panel
            SwitchPanel(m_waitPanel);

            // Get user database object
            User user = await User.Get();

            // Update user lobby value
            user.Lobby.Value = m_codeField.text.ToUpper();

            // Fetch lobby from cloud
            Lobby lobby = await Lobby.Get(user);

            // Attempt to join lobby
            bool success = CloudManager.JoinLobby(user, lobby, m_maxPlayers);

            if (success)
            {
                // Register lobby state change callback
                if (lobby.Users[0].UserId.Value != user.Id)
                    lobby.State.ValueChanged += LobbyStateChanged;

                // Show lobby panel
                m_startButton.SetActive(false);
                m_codeLabel.text = lobby.Id;
                SwitchPanel(m_lobbyPanel);
            }
            else
            {
                // Reset user lobby value
                user.Lobby.Value = null;

                // Show join panel
                m_codeField.text = "";
                SwitchPanel(m_joinPanel);
            }
        }
    }

    /// <summary>
    /// Called when the create button in the start panel is pressed.
    /// </summary>
    public async void CreateButtonPressed()
    {
        // Show wait panel
        SwitchPanel(m_waitPanel);

        // Attempt to create unique lobby code
        string code = await CloudManager.CreateLobbyCode();

        if (!string.IsNullOrEmpty(code))
        {
            // Create new lobby
            Lobby lobby = Lobby.Create(code);
            lobby.State.Value = (int)LobbyState.Lobby;

            // Get user database object
            User m_user = await User.Get();

            // Attempt to add user to lobby
            bool joinSuccess = CloudManager.JoinLobby(m_user, lobby, m_maxPlayers);

            if (joinSuccess)
            {
                // Set user lobby value
                m_user.Lobby.Value = lobby.Id;

                // Register lobby state change callback
                if (lobby.Users[0].UserId.Value != m_user.Id)
                    lobby.State.ValueChanged += LobbyStateChanged;

                // Show lobby panel
                m_startButton.SetActive(true);
                m_codeLabel.text = lobby.Id;
                SwitchPanel(m_lobbyPanel);
            }
            else
            {
                // Show start panel
                SwitchPanel(m_startPanel);
            }
        }
        else
        {
            // Show start panel
            SwitchPanel(m_startPanel);
        }
    }

    /// <summary>
    /// Called when the leave button in the lobby panel is pressed.
    /// </summary>
    public async void LeaveButtonPressed()
    {
        // Show wait panel
        SwitchPanel(m_waitPanel);

        // Get database objects
        User m_user = await User.Get();
        Lobby m_lobby = await Lobby.Get(m_user);

        // Deregister lobby state change callback
        if (m_lobby.Users[0].UserId.Value != m_user.Id)
            m_lobby.State.ValueChanged -= LobbyStateChanged;

        // Remove the user from the lobby
        CloudManager.LeaveLobby(m_user, m_lobby);

        // Show start panel
        m_codeLabel.text = "_____";
        SwitchPanel(m_startPanel);
    }

    /// <summary>
    /// Called when the start button in the lobby panel is pressed.
    /// </summary>
    public async void StartButtonPressed()
    {
        // Show wait panel
        SwitchPanel(m_waitPanel);

        // Get database objects
        User m_user = await User.Get();
        Lobby m_lobby = await Lobby.Get(m_user);

        // Assign users to their scenes
        CloudManager.AssignPlayerScenes(m_user, m_lobby);

        // Clear static data
        StaticInventory.Hints.Clear();

        // Set lobby state value
        m_lobby.State.Value = (int)LobbyState.InGame;
        LobbyStateChanged(m_lobby.State);
    }

    public void CodeFieldChanged(string s)
    {
        // Make code field text uppercase
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

    async void LobbyStateChanged(CloudNode<long> state)
    {
        // Get database objects
        User m_user = await User.Get();
        Lobby m_lobby = await Lobby.Get(m_user);

        if (state.Value.HasValue && (LobbyState)state.Value.Value == LobbyState.InGame)
        {
            // Get this user's scene
            int scene = (int)(CloudManager.OnlyUser(m_lobby, m_user).Scene.Value ?? 0);

            if (scene >= 1 && scene <= 4)
            {
                // Deregister lobby state change callback
                if (m_lobby.Users[0].UserId.Value != m_user.Id)
                    m_lobby.State.ValueChanged -= LobbyStateChanged;

                // Load this user's scene
                SceneManager.LoadScene(scene);
            }
        }
    }
}
