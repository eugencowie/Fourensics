using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class VotingWaitScene : MonoBehaviour
{
    private string m_roomCode;
    private Dictionary<string, string> m_votedPlayers = new Dictionary<string, string>();

    async void Start()
    {
        User m_user = await User.Get();
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
            foreach (var player in CloudManager.AllUsersStr(m_lobby)) m_votedPlayers[player] = "";
            await RegisterListeners();
            OnVoteChanged(m_lobby.Users.First(u => u.UserId.Value == m_user.Id).Vote);
        }
        else SceneManager.LoadScene("Lobby");
    }

    private async Task RegisterListeners()
    {
        User m_user = await User.Get();
        Lobby m_lobby = await Lobby.Get(m_user);

        foreach (LobbyUser user in m_lobby.Users.Where(u => u.UserId.Value != m_user.Id))
            user.Vote.ValueChanged += OnVoteChanged;
    }

    private async void OnVoteChanged(CloudNode entry)
    {
        if (entry.Value != null)
        {
            string value = entry.Value;

            if (!string.IsNullOrEmpty(value))
            {
                User m_user = await User.Get();
                Lobby m_lobby = await Lobby.Get(m_user);
                string player = m_lobby.Users.First(x => x.Id == entry.Key.Parent.Id).UserId.Value;
                m_votedPlayers[player] = value;
            }
        }

        if (!m_votedPlayers.Any(p => string.IsNullOrEmpty(p.Value)))
        {
            await DeregisterListeners();
            SceneManager.LoadScene("GameOver");
        }
    }

    private async Task DeregisterListeners()
    {
        User m_user = await User.Get();
        Lobby m_lobby = await Lobby.Get(m_user);

        foreach (LobbyUser user in m_lobby.Users.Where(u => u.UserId.Value != m_user.Id))
            user.Vote.ValueChanged -= OnVoteChanged;
    }
}
