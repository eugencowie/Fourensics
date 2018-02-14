using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class VotingWaitScene : MonoBehaviour
{
    private string m_roomCode;
    private Dictionary<string, string> m_votedPlayers = new Dictionary<string, string>();

    void Start()
    {
        if (LobbyScene.Lobby == null)
        {
            SceneManager.LoadScene("Lobby");
            return;
        }

        string room = LobbyScene.Lobby.Id;
        if (!string.IsNullOrEmpty(room))
        {
            m_roomCode = room;
            foreach (var player in CloudManager.AllUsers) m_votedPlayers[player] = "";
            RegisterListeners();
            OnVoteChanged(LobbyScene.Lobby.Users.First(u => u.UserId.Value == SignInScene.User.Id).Vote);
        }
        else SceneManager.LoadScene("Lobby");
    }

    private void RegisterListeners()
    {
        foreach (LobbyUser user in LobbyScene.Lobby.Users.Where(u => u.UserId.Value != SignInScene.User.Id))
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

    private async void DeregisterListeners()
    {
        foreach (LobbyUser user in LobbyScene.Lobby.Users.Where(u => u.UserId.Value != SignInScene.User.Id))
            user.Vote.ValueChanged -= OnVoteChanged;
    }
}
