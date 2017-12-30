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
            string[] players = await NetworkController.GetPlayers(m_roomCode);
            m_roomCode = room;
            foreach (var player in players) m_votedPlayers[player] = "";
            NetworkController.RegisterVoteChanged(room, OnVoteChanged);
        }
        else SceneManager.LoadScene("Lobby");
    }

    private void OnVoteChanged(OnlineDatabaseEntry entry, ValueChangedEventArgs args)
    {
        if (args.Snapshot.Exists)
        {
            string value = args.Snapshot.Value.ToString();

            if (!string.IsNullOrEmpty(value))
            {
                string[] key = entry.Key.Split('/');
                string player = key[1];
                m_votedPlayers[player] = value;

                if (!m_votedPlayers.Any(p => string.IsNullOrEmpty(p.Value)))
                {
                    NetworkController.DeregisterReadyChanged(m_roomCode);
                    SceneManager.LoadScene("GameOver");
                }
            }
        }
    }
}
