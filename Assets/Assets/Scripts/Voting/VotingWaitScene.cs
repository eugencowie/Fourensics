using Firebase.Database;
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
        string room = LobbyScene.Lobby.Id;
        if (!string.IsNullOrEmpty(room))
        {
            string[] players = OnlineManager.GetPlayers();
            m_roomCode = room;
            foreach (var player in players) m_votedPlayers[player] = "";
            OnlineManager.RegisterVoteChanged(OnVoteChanged);
        }
        else SceneManager.LoadScene("Lobby");
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

                if (!m_votedPlayers.Any(p => string.IsNullOrEmpty(p.Value)))
                {
                    OnlineManager.DeregisterVoteChanged(OnVoteChanged);
                    SceneManager.LoadScene("GameOver");
                }
            }
        }
    }
}
