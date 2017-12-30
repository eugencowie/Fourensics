using Firebase.Database;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class VotingWaitController : MonoBehaviour
{
    private OnlineManager NetworkController;
    private string m_roomCode;
    private Dictionary<string, string> m_votedPlayers = new Dictionary<string, string>();

    async void Start()
    {
        NetworkController = new OnlineManager();

        string room = await NetworkController.GetPlayerLobby();
        if (!string.IsNullOrEmpty(room))
        {
            string[] players = NetworkController.GetPlayers();
            m_roomCode = room;
            foreach (var player in players) m_votedPlayers[player] = "";
            NetworkController.RegisterVoteChanged(OnVoteChanged);
        }
        else SceneManager.LoadScene("Lobby");
    }

    private void OnVoteChanged(CloudNode entry)
    {
        if (entry.Exists())
        {
            string value = entry.Get();

            if (!string.IsNullOrEmpty(value))
            {
                string[] key = entry.Path.Split('/');
                string player = key[1];
                m_votedPlayers[player] = value;

                if (!m_votedPlayers.Any(p => string.IsNullOrEmpty(p.Value)))
                {
                    NetworkController.DeregisterVoteChanged(OnVoteChanged);
                    SceneManager.LoadScene("GameOver");
                }
            }
        }
    }
}
