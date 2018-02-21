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
        Debug.Log("LobbyScene.Start()");

        // Show please wait screen
        SwitchPanel(m_waitPanel);

        // Get database objects
        User m_user = await User.Get();
        Lobby m_lobby = await Lobby.Get(m_user);

        // Show main screen if user is not yet in a lobby or if user's lobby is invalid
        if (m_lobby == null || !m_lobby.State.Value.HasValue)
        {
            m_user.Lobby.Value = null;
            StaticClues.Reset();
            StaticInventory.Reset();
            StaticRoom.Reset();
            StaticSlot.Reset();
            StaticSuspects.Reset();
            SwitchPanel(m_startPanel);
            return;
        }

        // Show lobby screen
        m_codeLabel.text = m_lobby.Id;
        if (m_lobby.Users[0].UserId.Value != m_user.Id)
            m_lobby.State.ValueChanged += LobbyStateChanged;
        SwitchPanel(m_lobbyPanel);
    }

    /// <summary>
    /// Called when the join button in the start panel is pressed.
    /// </summary>
    public void JoinButtonPressed()
    {
        Debug.Log("LobbyScene.JoinButtonPressed()");

        SwitchPanel(m_joinPanel);
    }

    /// <summary>
    /// Called when the join button in the start panel is pressed.
    /// </summary>
    public void BackButtonPressed()
    {
        Debug.Log("LobbyScene.BackButtonPressed()");

        SwitchPanel(m_startPanel);
    }

    /// <summary>
    /// Called when the submit button in the join panel is pressed.
    /// </summary>
    public async void SubmitButtonPressed()
    {
        Debug.Log("LobbyScene.SubmitButtonPressed()");

        User m_user = await User.Get();
        Lobby m_lobby = await Lobby.Get(m_user);

        if (!string.IsNullOrEmpty(m_codeField.text))
        {
            SwitchPanel(m_waitPanel);

            // Fetch lobby from cloud
            Lobby lobby = await Cloud.Fetch<Lobby>("lobbies", m_codeField.text.ToUpper());

            bool success = CloudManager.JoinLobby(m_user, lobby, m_maxPlayers);
            if (!success)
            {
                m_codeField.text = "";
                SwitchPanel(m_joinPanel);
            }
            else
            {
                m_lobby = lobby;
                m_user.Lobby.Value = m_lobby.Id;
                m_codeLabel.text = m_lobby.Id;
                if (m_lobby.Users[0].UserId.Value != m_user.Id)
                    m_lobby.State.ValueChanged += LobbyStateChanged;
                m_startButton.SetActive(false);
                SwitchPanel(m_lobbyPanel);
            }
        }
    }

    /// <summary>
    /// Called when the create button in the start panel is pressed.
    /// </summary>
    public async void CreateButtonPressed()
    {
        Debug.Log("LobbyScene.CreateButtonPressed()");

        SwitchPanel(m_waitPanel);

        User m_user = await User.Get();
        Lobby m_lobby = await Lobby.Get(m_user);

        string code = await CloudManager.CreateLobbyCode();
        if (string.IsNullOrEmpty(code)) SwitchPanel(m_startPanel);
        else
        {
            Lobby lobby = Lobby.Create(code);
            lobby.State.Value = (int)LobbyState.Lobby;

            bool joinSuccess = CloudManager.JoinLobby(m_user, lobby, m_maxPlayers);
            if (!joinSuccess) SwitchPanel(m_startPanel);
            else
            {
                m_lobby = lobby;
                m_user.Lobby.Value = m_lobby.Id;
                m_codeLabel.text = m_lobby.Id;
                if (m_lobby.Users[0].UserId.Value != m_user.Id)
                    m_lobby.State.ValueChanged += LobbyStateChanged;
                m_startButton.SetActive(true);
                SwitchPanel(m_lobbyPanel);
            }
        }
    }

    /// <summary>
    /// Called when the leave button in the lobby panel is pressed.
    /// </summary>
    public async void LeaveButtonPressed()
    {
        Debug.Log("LobbyScene.LeaveButtonPressed()");

        SwitchPanel(m_waitPanel);

        User m_user = await User.Get();
        Lobby m_lobby = await Lobby.Get(m_user);

        if (m_lobby.Users[0].UserId.Value != m_user.Id)
            m_lobby.State.ValueChanged -= LobbyStateChanged;
        CloudManager.LeaveLobby(m_user, m_lobby);
        m_codeLabel.text = "_____";
        SwitchPanel(m_startPanel);
    }

    /// <summary>
    /// Called when the start button in the lobby panel is pressed.
    /// </summary>
    public async void StartButtonPressed()
    {
        Debug.Log("LobbyScene.StartButtonPressed()");

        SwitchPanel(m_waitPanel);

        User m_user = await User.Get();
        Lobby m_lobby = await Lobby.Get(m_user);

        CloudManager.AssignPlayerScenes(m_user, m_lobby);
        StaticInventory.Hints.Clear();
        m_lobby.State.Value = (int)LobbyState.InGame;
        LobbyStateChanged(m_lobby.State);
    }

    public void CodeFieldChanged(string s)
    {
        Debug.Log($"LobbyScene.CodeFieldChanged(\"{s}\")");

        m_codeField.text = m_codeField.text.ToUpper();
    }

    void SwitchPanel(GameObject panel)
    {
        Debug.Log($"LobbyScene.SwitchPanel(\"{panel.name}\")");

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
        Debug.Log($"LobbyScene.LobbyStateChanged(\"{state.Value}\")");

        User m_user = await User.Get();
        Lobby m_lobby = await Lobby.Get(m_user);

        if (state.Value.HasValue && (LobbyState)state.Value.Value == LobbyState.InGame)
        {
            int scene = (int)(m_lobby.Users.First(u => u.UserId.Value == m_user.Id).Scene.Value ?? 0);
            if (scene >= 1 && scene <= 4)
            {
                if (m_lobby.Users[0].UserId.Value != m_user.Id)
                    m_lobby.State.ValueChanged -= LobbyStateChanged;

                SceneManager.LoadScene(scene);
            }
        }
    }
}
