using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

class RetryScene : MonoBehaviour
{
    [SerializeField] Text m_playersLabel = null;

    async void Start()
    {
        // Get database objects
        User user; try { user = await User.Get(); } catch { SceneManager.LoadScene("SignIn"); return; }
        Lobby lobby = await Lobby.Get(user);

        // Register lobby user id change callbacks
        foreach (LobbyUser lobbyUser in lobby.Users)
            lobbyUser.UserId.ValueChanged += LobbyUserIdChanged;

        LobbyUserIdChanged(CloudManager.OnlyUser(lobby, user).UserId);
    }

    async void LobbyUserIdChanged(CloudNode userId)
    {
        // Get database objects
        User user; try { user = await User.Get(); } catch { SceneManager.LoadScene("SignIn"); return; }
        Lobby lobby = await Lobby.Get(user);

        // Get number of players in lobby
        int playerCount = lobby.Users.Count(x => !string.IsNullOrWhiteSpace(x.UserId.Value));

        // Set players text
        m_playersLabel.text = $"{playerCount} / {LobbyScene.MaxPlayers}";
    }

    public async void ExitButtonPressed()
    {
        // Get database objects
        User user; try { user = await User.Get(); } catch { SceneManager.LoadScene("SignIn"); return; }
        Lobby lobby = await Lobby.Get(user);

        // Deregister lobby user id change callbacks
        foreach (LobbyUser lobbyUser in lobby.Users)
            lobbyUser.UserId.ValueChanged -= LobbyUserIdChanged;

        // Remove user from lobby
        CloudManager.LeaveLobby(user, lobby);

        // Load lobby scene
        SceneManager.LoadScene("Lobby");
    }

    public async void StartButtonPressed()
    {
        // Get database objects
        User user; try { user = await User.Get(); } catch { SceneManager.LoadScene("SignIn"); return; }
        Lobby lobby = await Lobby.Get(user);

        // Set lobby state value
        lobby.State.Value = (int)LobbyState.InGame;
        LobbyStateChanged(lobby.State);
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
                // Get this user's scene
                int scene = (int)(CloudManager.OnlyUser(lobby, user).Scene.Value ?? 0);

                if (scene >= 1 && scene <= LobbyScene.ScenesPerCase)
                {
                    // Deregister lobby user id change callbacks
                    foreach (LobbyUser lobbyUser in lobby.Users)
                        lobbyUser.UserId.ValueChanged -= LobbyUserIdChanged;

                    // Load this user's scene
                    SceneManager.LoadScene(((caseNb - 1) * LobbyScene.ScenesPerCase) + scene);
                }
            }
        }
    }
}
