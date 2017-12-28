using Firebase.Database;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class GameOverController : MonoBehaviour
{
    public VideoPlayer WinVideo;
    public VideoPlayer LoseVideo;
    public GameObject ResetButton;

    public Text WaitText;
    public Text WinText;
    public Text LoseText;

    [Range(0, 100)]
    public int RequiredVotePercentage = 51;

    private OnlineManager NetworkController;
    private string m_roomCode;
    private Dictionary<string, string> m_votedPlayers = new Dictionary<string, string>();

    private Text m_winOrLoseText;

    private void Start()
    {
        NetworkController = new OnlineManager();

        ResetButton.SetActive(false);

        WinVideo.loopPointReached += VideoLoopPointReached;
        LoseVideo.loopPointReached += VideoLoopPointReached;

        NetworkController.GetPlayerLobby(room => {
            if (!string.IsNullOrEmpty(room)) {
                NetworkController.GetPlayers(room, players => {
                    m_roomCode = room;
                    foreach (var player in players) m_votedPlayers[player] = "";
                    NetworkController.RegisterVoteChanged(room, OnVoteChanged);
                });
            }
            else SceneManager.LoadScene("Communication Detective/Scenes/Lobby");
        });
    }

    private void VideoLoopPointReached(VideoPlayer source)
    {
        //source.gameObject.SetActive(false);
        m_winOrLoseText.gameObject.SetActive(true);
        ResetButton.SetActive(true);
    }

    public void ResetButtonPressed()
    {
        NetworkController.LeaveLobby(m_roomCode, _ => {
            SceneManager.LoadScene("Communication Detective/Scenes/Lobby");
        });
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
                    float correctAnswers = m_votedPlayers.Count(p => p.Value == "Caleb Holden");
                    float totalAnswers = m_votedPlayers.Count;

                    float percentage = correctAnswers / totalAnswers;
                    float requiredPercentage = RequiredVotePercentage / 100.0f;

                    if (percentage >= requiredPercentage)
                    {
                        string yourVote = m_votedPlayers[OnlineManager.GetPlayerId()];
                        m_votedPlayers.Remove(OnlineManager.GetPlayerId());

                        WinText.text += "\n\nYou voted for " + yourVote;
                        for (int i=0; i<m_votedPlayers.Count; i++)
                        {
                            WinText.text += "\nPlayer " + (i+2) + " voted for " + m_votedPlayers.ElementAt(i).Value;
                        }
                        WaitText.gameObject.SetActive(false);
                        WinVideo.gameObject.SetActive(true);

                        m_winOrLoseText = WinText;

                        m_votedPlayers[OnlineManager.GetPlayerId()] = yourVote;
                    }
                    else
                    {
                        WaitText.gameObject.SetActive(false);
                        LoseVideo.gameObject.SetActive(true);

                        m_winOrLoseText = LoseText;
                    }
                }
            }
        }
    }
}
