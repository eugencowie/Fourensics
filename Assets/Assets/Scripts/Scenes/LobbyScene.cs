using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

enum LobbyState { Lobby, InGame, Voting, Finished }

class LobbyScene : MonoBehaviour
{
    private static Lobby m_lobbyTMP = null;

    public static async Task<Lobby> Lobby(User user)
    {
        if (string.IsNullOrWhiteSpace(user.Lobby.Value))
        {
            // User is not in a lobby
            m_lobbyTMP = null;
        }
        else if (m_lobbyTMP == null)
        {
            // Get user's lobby
            m_lobbyTMP = await Cloud.Fetch<Lobby>("lobbies", user.Lobby.Value);
        }

        return m_lobbyTMP;
    }

    [SerializeField] Text m_codeLabel = null;
    [SerializeField] InputField m_codeField = null;
    [SerializeField] Text m_playersLabel = null;
    [SerializeField] GameObject m_startPanel = null;
    [SerializeField] GameObject m_joinPanel = null;
    [SerializeField] GameObject m_lobbyPanel = null;
    [SerializeField] GameObject m_waitPanel = null;
    [SerializeField] GameObject m_startButton = null;

    const int m_maxPlayers = 4;

    User m_user = null;
    Lobby m_lobby = null;

    async void Start()
    {
        // Show please wait screen
        SwitchPanel(m_waitPanel);

        // Get database objects
        m_user = await SignInScene.User();
        m_lobby = await Lobby(m_user);

        // Show main screen if user is not yet in a lobby
        if (m_lobby == null)
        {
            StaticClues.Reset();
            StaticInventory.Reset();
            StaticRoom.Reset();
            StaticSlot.Reset();
            StaticSuspects.Reset();
            SwitchPanel(m_startPanel);
            return;
        }

        // Show main screen if user's lobby is invalid
        if (!m_lobby.State.Value.HasValue)
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
        RegisterOnPlayersChanged();
        RegisterOnLobbyStateChanged();
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

            bool joinSuccess = CloudManager.JoinLobby(m_user, lobby, m_maxPlayers);
            if (!joinSuccess) SwitchPanel(m_startPanel);
            else
            {
                m_lobby = lobby;
                m_user.Lobby.Value = m_lobby.Id;
                m_codeLabel.text = m_lobby.Id;
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
    public void StartButtonPressed()
    {
        SwitchPanel(m_waitPanel);

        CloudManager.AssignPlayerScenes(m_user, m_lobby);
        StaticInventory.Hints.Clear();
        m_lobby.State.Value = (int)LobbyState.InGame;
    }

    /// <summary>
    /// Called when the leave button in the lobby panel is pressed.
    /// </summary>
    public void LeaveButtonPressed()
    {
        SwitchPanel(m_waitPanel);

        DeregisterOnLobbyStateChanged();
        DeregisterOnPlayersChanged();
        CloudManager.LeaveLobby(m_user, m_lobby);
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
        foreach (CloudNode user in m_lobby.Users.Select(u => u.UserId))
            user.ValueChanged += OnPlayersChanged;
    }

    void DeregisterOnPlayersChanged()
    {
        foreach (CloudNode user in m_lobby.Users.Select(u => u.UserId))
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
        m_lobby.State.ValueChanged += OnLobbyStateChanged;
    }

    void DeregisterOnLobbyStateChanged()
    {
        m_lobby.State.ValueChanged -= OnLobbyStateChanged;
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
                    int scene = (int)(m_lobby.Users.First(u => u.UserId.Value == m_user.Id).Scene.Value ?? 0);
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
