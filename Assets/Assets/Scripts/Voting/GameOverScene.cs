using Firebase.Database;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class GameOverScene : MonoBehaviour
{
    public VideoPlayer WinVideo;
    public VideoPlayer LoseVideo;
    public GameObject ResetButton;

    public Text WaitText;
    public Text WinText;
    public Text LoseText;

    [Range(0, 100)]
    public int RequiredVotePercentage = 51;

    private string m_roomCode;
    private Dictionary<string, string> m_votedPlayers = new Dictionary<string, string>();

    private Text m_winOrLoseText;

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

        ResetButton.SetActive(false);

        WinVideo.loopPointReached += VideoLoopPointReached;
        LoseVideo.loopPointReached += VideoLoopPointReached;

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
        foreach (LobbyUser user in m_lobby.Users)
            user.Vote.ValueChanged += OnVoteChanged;
    }

    private void VideoLoopPointReached(VideoPlayer source)
    {
        //source.gameObject.SetActive(false);
        m_winOrLoseText.gameObject.SetActive(true);
        ResetButton.SetActive(true);
    }

    public void ResetButtonPressed()
    {
        CloudManager.LeaveLobby(m_user, m_lobby);
        SceneManager.LoadScene("Lobby");
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
                    float correctAnswers = m_votedPlayers.Count(p => p.Value == "Caleb Holden");
                    float totalAnswers = m_votedPlayers.Count;

                    float percentage = correctAnswers / totalAnswers;
                    float requiredPercentage = RequiredVotePercentage / 100.0f;

                    if (percentage >= requiredPercentage)
                    {
                        string yourVote = m_votedPlayers[m_user.Id];
                        m_votedPlayers.Remove(m_user.Id);

                        WinText.text += "\n\nYou voted for " + yourVote;
                        for (int i = 0; i < m_votedPlayers.Count; i++)
                        {
                            WinText.text += "\nPlayer " + (i + 2) + " voted for " + m_votedPlayers.ElementAt(i).Value;
                        }
                        WaitText.gameObject.SetActive(false);
                        WinVideo.gameObject.SetActive(true);

                        m_winOrLoseText = WinText;

                        m_votedPlayers[m_user.Id] = yourVote;
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
