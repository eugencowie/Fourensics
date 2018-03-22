using Firebase.Database;
using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
class LobbyPanels
{
    public GameObject Main = null;
    public GameObject Create = null;
    public GameObject Join = null;
    public GameObject Lobby = null;
    public GameObject Wait = null;
    public GameObject[] All => new GameObject[] { Main, Create, Join, Lobby, Wait };
}

class LobbyScene : MonoBehaviour
{
    [SerializeField] Text m_codeLabel = null;
    [SerializeField] Text m_playersLabel = null;
    [SerializeField] GameObject m_startButton = null;
    [SerializeField] GameObject m_joinButtonTemplate = null;

    [SerializeField] LobbyPanels m_panels = null;

    const int m_maxPlayers = 4;

    async void Start()
    {
        // Show wait panel
        SwitchPanel(m_panels.Wait);

        // Get database objects
        User user; try { user = await User.Get(); } catch { SceneManager.LoadScene("SignIn"); return; }
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
            SwitchPanel(m_panels.Main);
        }
        else
        {
            // Register callbacks
            await RegisterCallbacks();

            // Show lobby panel
            m_codeLabel.text = lobby.Id;
            LobbyUserIdChanged(CloudManager.OnlyUser(lobby, user).UserId);
            SwitchPanel(m_panels.Lobby);
        }
    }

    async Task RegisterCallbacks()
    {
        // Get database objects
        User user; try { user = await User.Get(); } catch { SceneManager.LoadScene("SignIn"); return; }
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
        User user; try { user = await User.Get(); } catch { SceneManager.LoadScene("SignIn"); return; }
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
    public async void JoinButtonPressed()
    {
        // Destroy all existing buttons
        foreach (Transform t in m_joinButtonTemplate.transform.parent)
            if (t.gameObject.activeSelf && t.gameObject.name != "Back button")
                Destroy(t.gameObject);

        // Get lobbies database entry
        DataSnapshot lobbyData = await FirebaseDatabase.DefaultInstance.RootReference.Child("lobbies").GetValueAsync();
        
        foreach (DataSnapshot lobby in lobbyData.Children)
        {
            // Create new button
            GameObject newButton = Instantiate(m_joinButtonTemplate, m_joinButtonTemplate.transform.parent);
            newButton.SetActive(true);
            newButton.name = lobby.Key;

            // Add on-click listener
            newButton.GetComponent<Button>().onClick.AddListener(() => LobbyButtonPressed(lobby.Key));
            
            // Set button text
            foreach (Transform t in newButton.transform)
                if (t.gameObject.GetComponent<Text>() != null)
                    t.gameObject.GetComponent<Text>().text = lobby.Key;
        }

        // Show join panel
        SwitchPanel(m_panels.Join);
    }

    /// <summary>
    /// Called when the join button in the start panel is pressed.
    /// </summary>
    public void BackButtonPressed()
    {
        // Show start panel
        SwitchPanel(m_panels.Main);
    }

    /// <summary>
    /// Called when the submit button in the join panel is pressed.
    /// </summary>
    public async void LobbyButtonPressed(string code)
    {
        if (!string.IsNullOrEmpty(code))
        {
            // Show wait panel
            SwitchPanel(m_panels.Wait);

            // Get user database object
            User user; try { user = await User.Get(); } catch { SceneManager.LoadScene("SignIn"); return; }

            // Update user lobby value
            user.Lobby.Value = code.ToUpper();

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
                SwitchPanel(m_panels.Lobby);
            }
            else
            {
                // Reset user lobby value
                user.Lobby.Value = null;

                // Show join panel
                JoinButtonPressed();
            }
        }
    }

    /// <summary>
    /// Called when the create button in the start panel is pressed.
    /// </summary>
    public void CreateButtonPressed()
    {
        // Show create panel
        SwitchPanel(m_panels.Create);
    }

    /// <summary>
    /// Called when one of the case buttons in the create panel is pressed.
    /// </summary>
    public async Task CaseButtonPressed(int caseNb)
    {
        // Show wait panel
        SwitchPanel(m_panels.Wait);

        // Attempt to create unique lobby code
        string code = await CloudManager.CreateLobbyCode();

        if (!string.IsNullOrEmpty(code))
        {
            // Create new lobby
            Lobby lobby = Lobby.Create(code);
            lobby.State.Value = (int)LobbyState.Lobby;
            lobby.Case.Value = caseNb;

            // Get user database object
            User user; try { user = await User.Get(); } catch { SceneManager.LoadScene("SignIn"); return; }

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
                SwitchPanel(m_panels.Lobby);
            }
            else
            {
                // Show start panel
                SwitchPanel(m_panels.Main);
            }
        }
        else
        {
            // Show start panel
            SwitchPanel(m_panels.Main);
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
        SwitchPanel(m_panels.Wait);

        // Get database objects
        User user; try { user = await User.Get(); } catch { SceneManager.LoadScene("SignIn"); return; }
        Lobby lobby = await Lobby.Get(user);

        // Deregister callbacks
        await DeregisterCallbacks();

        // Remove the user from the lobby
        CloudManager.LeaveLobby(user, lobby);

        // Show start panel
        m_codeLabel.text = "_____";
        SwitchPanel(m_panels.Main);
    }

    /// <summary>
    /// Called when the start button in the lobby panel is pressed.
    /// </summary>
    public async void StartButtonPressed()
    {
        // Show wait panel
        SwitchPanel(m_panels.Wait);

        // Get database objects
        User user; try { user = await User.Get(); } catch { SceneManager.LoadScene("SignIn"); return; }
        Lobby lobby = await Lobby.Get(user);

        // Assign users to their scenes
        CloudManager.AssignPlayerScenes(user, lobby);

        // Clear static data
        StaticInventory.Hints.Clear();

        // Set lobby state value
        lobby.State.Value = (int)LobbyState.InGame;
        LobbyStateChanged(lobby.State);
    }

    void SwitchPanel(GameObject panel)
    {
        // Disable all panels
        foreach (var p in m_panels.All)
            p.SetActive(false);

        // Enable specified panel
        panel.SetActive(true);
    }

    async void LobbyUserIdChanged(CloudNode userId)
    {
        // Get database objects
        User user; try { user = await User.Get(); } catch { SceneManager.LoadScene("SignIn"); return; }
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
            User user; try { user = await User.Get(); } catch { SceneManager.LoadScene("SignIn"); return; }
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
