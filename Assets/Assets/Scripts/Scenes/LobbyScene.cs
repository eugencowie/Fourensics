using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

class LobbyScene : MonoBehaviour
{
    [SerializeField] Text m_codeLabel = null;
    [SerializeField] Text m_playersLabel = null;
    [SerializeField] InputField m_codeField = null;

    [SerializeField] GameObject m_startPanel = null;
    [SerializeField] GameObject m_createPanel = null;
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
            // Register callbacks
            await RegisterCallbacks();

            // Show lobby panel
            m_codeLabel.text = lobby.Id;
            LobbyUserIdChanged(CloudManager.OnlyUser(lobby, user).UserId);
            SwitchPanel(m_lobbyPanel);
        }
    }

    async Task RegisterCallbacks()
    {
        // Get database objects
        User user = await User.Get();
        Lobby lobby = await Lobby.Get(user);

        // Register lobby state change callback
        if (lobby.Users[0].UserId.Value != user.Id)
            lobby.State.ValueChanged += LobbyStateChanged;

        // Register lobby user id change callbacks
        foreach (LobbyUser lobbyUser in lobby.Users)
            lobbyUser.UserId.ValueChanged += LobbyUserIdChanged;
    }

    async Task DeregisterCallbacks()
    {
        // Get database objects
        User user = await User.Get();
        Lobby lobby = await Lobby.Get(user);

        // Deregister lobby state change callback
        if (lobby.Users[0].UserId.Value != user.Id)
            lobby.State.ValueChanged -= LobbyStateChanged;

        // Deregister lobby user id change callbacks
        foreach (LobbyUser lobbyUser in lobby.Users)
            lobbyUser.UserId.ValueChanged -= LobbyUserIdChanged;
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
                // Register callbacks
                await RegisterCallbacks();

                // Show lobby panel
                m_startButton.SetActive(false);
                m_codeLabel.text = lobby.Id;
                LobbyUserIdChanged(CloudManager.OnlyUser(lobby, user).UserId);
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
    public void CreateButtonPressed()
    {
        // Show create panel
        SwitchPanel(m_createPanel);
    }

    /// <summary>
    /// Called when one of the case buttons in the create panel is pressed.
    /// </summary>
    public async Task CaseButtonPressed(int caseNb)
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
            lobby.Case.Value = caseNb;

            // Get user database object
            User user = await User.Get();

            // Attempt to add user to lobby
            bool joinSuccess = CloudManager.JoinLobby(user, lobby, m_maxPlayers);

            if (joinSuccess)
            {
                // Set user lobby value
                user.Lobby.Value = lobby.Id;

                // Register callbacks
                await RegisterCallbacks();

                // Show lobby panel
                m_startButton.SetActive(true);
                m_codeLabel.text = lobby.Id;
                LobbyUserIdChanged(CloudManager.OnlyUser(lobby, user).UserId);
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

    public async void Case1ButtonPressed() => await CaseButtonPressed(1);
    public async void Case2ButtonPressed() => await CaseButtonPressed(2);

    /// <summary>
    /// Called when the leave button in the lobby panel is pressed.
    /// </summary>
    public async void LeaveButtonPressed()
    {
        // Show wait panel
        SwitchPanel(m_waitPanel);

        // Get database objects
        User user = await User.Get();
        Lobby lobby = await Lobby.Get(user);

        // Deregister callbacks
        await DeregisterCallbacks();

        // Remove the user from the lobby
        CloudManager.LeaveLobby(user, lobby);

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
        User user = await User.Get();
        Lobby lobby = await Lobby.Get(user);

        // Assign users to their scenes
        CloudManager.AssignPlayerScenes(user, lobby);

        // Clear static data
        StaticInventory.Hints.Clear();

        // Set lobby state value
        lobby.State.Value = (int)LobbyState.InGame;
        LobbyStateChanged(lobby.State);
    }

    public void CodeFieldChanged(string s)
    {
        // Make code field text uppercase
        m_codeField.text = m_codeField.text.ToUpper();
    }

    void SwitchPanel(GameObject panel)
    {
        // Disable all panels
        foreach (var p in new GameObject[] { m_startPanel, m_createPanel, m_joinPanel, m_lobbyPanel, m_waitPanel })
        {
            p.SetActive(false);
        }

        // Enable specified panel
        panel.SetActive(true);
    }

    async void LobbyUserIdChanged(CloudNode userId)
    {
        // Get database objects
        User user = await User.Get();
        Lobby lobby = await Lobby.Get(user);

        // Get number of players in lobby
        int playerCount = lobby.Users.Count(x => !string.IsNullOrWhiteSpace(x.UserId.Value));

        // Set players text
        m_playersLabel.text = $"{playerCount} / {m_maxPlayers}";
    }

    async void LobbyStateChanged(CloudNode<long> state)
    {
        if (state.Value.HasValue && (LobbyState)state.Value.Value == LobbyState.InGame)
        {
            // Get database objects
            User user = await User.Get();
            Lobby lobby = await Lobby.Get(user);

            // Get lobby case number
            int caseNb = (int)(lobby.Case.Value ?? 0);

            if (caseNb >= 1 && caseNb <= 2)
            {
                const int scenesPerCase = 4;

                // Get this user's scene
                int scene = (int)(CloudManager.OnlyUser(lobby, user).Scene.Value ?? 0);

                if (scene >= 1 && scene <= scenesPerCase)
                {
                    // Deregister callbacks
                    await DeregisterCallbacks();

                    // Load this user's scene
                    SceneManager.LoadScene(((caseNb - 1) * scenesPerCase) + scene);
                }
            }
        }
    }
}
