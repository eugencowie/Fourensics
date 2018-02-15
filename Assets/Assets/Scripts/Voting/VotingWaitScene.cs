using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class VotingWaitScene : MonoBehaviour
{
    private string m_roomCode;
    private Dictionary<string, string> m_votedPlayers = new Dictionary<string, string>();

    User m_user = null;
    Lobby m_lobby = null;

    async void Start()
    {
        m_user = await SignInScene.User();
        m_lobby = await LobbyScene.Lobby(m_user);

        if (m_lobby == null)
        {
            SceneManager.LoadScene("Lobby");
            return;
        }

        string room = m_lobby.Id;
        if (!string.IsNullOrEmpty(room))
        {
            m_roomCode = room;
            foreach (var player in CloudManager.AllUsers(m_lobby)) m_votedPlayers[player] = "";
            RegisterListeners();
            OnVoteChanged(m_lobby.Users.First(u => u.UserId.Value == m_user.Id).Vote);
        }
        else SceneManager.LoadScene("Lobby");
    }

    private void RegisterListeners()
    {
        foreach (LobbyUser user in m_lobby.Users.Where(u => u.UserId.Value != m_user.Id))
            user.Vote.ValueChanged += OnVoteChanged;
    }

    private void OnVoteChanged(CloudNode entry)
    {
        if (entry.Value != null)
        {
            string value = entry.Value;

            if (!string.IsNullOrEmpty(value))
            {
                string[] key = entry.Key.Split('/');
                string player = key[1];
                m_votedPlayers[player] = value;
            }
        }

        if (!m_votedPlayers.Any(p => string.IsNullOrEmpty(p.Value)))
        {
            DeregisterListeners();
            SceneManager.LoadScene("GameOver");
        }
    }

    private void DeregisterListeners()
    {
        foreach (LobbyUser user in m_lobby.Users.Where(u => u.UserId.Value != m_user.Id))
            user.Vote.ValueChanged -= OnVoteChanged;
    }
}
