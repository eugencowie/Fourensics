using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

class GameOverScene : MonoBehaviour
{
    [Serializable]
    struct Case
    {
        public string CorrectAnswer;
        public Text WinText;
        public Text LoseText;
    }

    [SerializeField] VideoPlayer m_winVideo;
    [SerializeField] VideoPlayer m_loseVideo;
    [SerializeField] Button m_resetButton;

    [SerializeField] Text m_waitText;
    [SerializeField] Case[] m_cases;

    [Range(0, 100)]
    [SerializeField]
    int RequiredVotePercentage = 51;

    string m_roomCode;

    Text m_winOrLoseText;

    async void Start()
    {
        // Get database objects
        User m_user; try { m_user = await User.Get(); } catch { SceneManager.LoadScene("SignIn"); return; }
        Lobby m_lobby = await Lobby.Get(m_user);

        if (m_lobby == null && !string.IsNullOrEmpty(m_lobby.Id))
        {
            // Load lobby scene
            SceneManager.LoadScene("Lobby");
            return;
        }

        // Disable reset button
        m_resetButton.gameObject.SetActive(false);

        // Set video end event handlers
        m_winVideo.loopPointReached += VideoLoopPointReached;
        m_loseVideo.loopPointReached += VideoLoopPointReached;

        // Register vote changed event handlers
        foreach (LobbyUser user in CloudManager.OtherUsers(m_lobby, m_user))
            user.Vote.ValueChanged += OnVoteChanged;

        // Trigger vote changed event handler for current user
        OnVoteChanged(CloudManager.OnlyUser(m_lobby, m_user).Vote);
    }

    public async void ResetButtonPressed()
    {
        // Get database objects
        User m_user; try { m_user = await User.Get(); } catch { SceneManager.LoadScene("SignIn"); return; }
        Lobby m_lobby = await Lobby.Get(m_user);

        // Deregister vote changed event handlers
        foreach (LobbyUser user in CloudManager.OtherUsers(m_lobby, m_user))
            user.Vote.ValueChanged -= OnVoteChanged;

        // Remove user from lobby
        CloudManager.LeaveLobby(m_user, m_lobby);

        // Load lobby scene
        SceneManager.LoadScene("Lobby");
    }

    void VideoLoopPointReached(VideoPlayer source)
    {
        // Enable win/lose text
        m_winOrLoseText.gameObject.SetActive(true);

        // Enable reset button
        m_resetButton.gameObject.SetActive(true);
    }

    async void OnVoteChanged(CloudNode entry)
    {
        if (entry.Value != null && !string.IsNullOrEmpty(entry.Value))
        {
            // Get database objects
            User user; try { user = await User.Get(); } catch { SceneManager.LoadScene("SignIn"); return; }
            Lobby lobby = await Lobby.Get(user);

            // Get lobby case number
            int caseNb = (int)(lobby.Case.Value ?? 0);

            if (caseNb >= 1 && caseNb <= 2 && (caseNb - 1) < m_cases.Length)
            {
                // Check if everyone has voted
                if (CloudManager.AllUsers(lobby).All(x => !string.IsNullOrWhiteSpace(x.Vote.Value)))
                {
                    // Get number of correct answers and total answers
                    float correctAnswers = CloudManager.AllUsers(lobby).Count(x => x.Vote.Value == m_cases[caseNb - 1].CorrectAnswer);
                    float totalAnswers = CloudManager.AllUsers(lobby).Count();

                    // Calculate percentage of correct answers
                    float percentage = correctAnswers / totalAnswers;
                    float requiredPercentage = RequiredVotePercentage / 100.0f;

                    // Check if enough players have voted correctly
                    if (percentage >= requiredPercentage)
                    {
                        // Append current users vote to win text
                        m_cases[caseNb - 1].WinText.text += $"\n\nYou voted for {CloudManager.OnlyUser(lobby, user).Vote.Value}";

                        // Append other users votes to win text
                        int counter = 2;
                        foreach (LobbyUser u in CloudManager.OtherUsers(lobby, user))
                        {
                            m_cases[caseNb - 1].WinText.text += $"\nPlayer {counter} voted for {u.Vote.Value}";
                            counter++;
                        }

                        // Hide wait text
                        m_waitText.gameObject.SetActive(false);

                        // Show win video
                        m_winVideo.gameObject.SetActive(true);

                        // Set win/lose text
                        m_winOrLoseText = m_cases[caseNb - 1].WinText;
                    }
                    else
                    {
                        // Hide wait text
                        m_waitText.gameObject.SetActive(false);

                        // Show lose video
                        m_loseVideo.gameObject.SetActive(true);

                        // Set win/lose text
                        m_winOrLoseText = m_cases[caseNb - 1].LoseText;
                    }
                }
            }
        }
    }
}
