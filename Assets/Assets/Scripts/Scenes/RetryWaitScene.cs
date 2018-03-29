using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RetryWaitScene : MonoBehaviour
{
    private string m_roomCode;

    async void Start()
    {
        User m_user; try { m_user = await User.Get(); } catch { SceneManager.LoadScene("SignIn"); return; }
        Lobby m_lobby = await Lobby.Get(m_user);

        if (m_lobby == null)
        {
            SceneManager.LoadScene("Lobby");
            return;
        }

        string room = m_lobby.Id;
        if (!string.IsNullOrEmpty(room))
        {
            m_roomCode = room;
            await RegisterListeners();
            OnVoteChanged(CloudManager.OnlyUser(m_lobby, m_user).Vote);
        }
        else SceneManager.LoadScene("Lobby");
    }

    private async Task RegisterListeners()
    {
        User m_user; try { m_user = await User.Get(); } catch { SceneManager.LoadScene("SignIn"); return; }
        Lobby m_lobby = await Lobby.Get(m_user);

        foreach (LobbyUser user in CloudManager.OtherUsers(m_lobby, m_user))
            user.Vote.ValueChanged += OnVoteChanged;
    }

    private async Task DeregisterListeners()
    {
        User m_user; try { m_user = await User.Get(); } catch { SceneManager.LoadScene("SignIn"); return; }
        Lobby m_lobby = await Lobby.Get(m_user);

        foreach (LobbyUser user in CloudManager.OtherUsers(m_lobby, m_user))
            user.Vote.ValueChanged -= OnVoteChanged;
    }

    private async void OnVoteChanged(CloudNode entry)
    {
        if (entry.Value != null)
        {
            string value = entry.Value;

            if (!string.IsNullOrEmpty(value))
            {
                User m_user; try { m_user = await User.Get(); } catch { SceneManager.LoadScene("SignIn"); return; }
                Lobby m_lobby = await Lobby.Get(m_user);

                bool everyoneVoted = CloudManager.AllUsers(m_lobby).All(x => !string.IsNullOrWhiteSpace(x.Vote.Value));

                if (everyoneVoted)
                {
                    await DeregisterListeners();
                    SceneManager.LoadScene("GameOver");
                }
            }
        }
    }
}
