using System;
using System.Collections.Generic;
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
    [SerializeField] Button m_retryButton;
    [SerializeField] Button m_resetButton;

    [SerializeField] Text m_waitText;
    [SerializeField] Case[] m_cases;

    [Range(0, 100)]
    [SerializeField]
    int RequiredVotePercentage = 51;

    string m_roomCode;

    Text m_winOrLoseText;
    List<Button> m_winOrLoseButtons = new List<Button>();

    async void Start()
    {
        // Disable reset button
        m_resetButton.gameObject.SetActive(false);

        // Get database objects
        User m_user = await User.Get(); if (m_user == null) { SceneManager.LoadSceneAsync("SignIn"); return; }
        Lobby m_lobby = await Lobby.Get(m_user);

        if (m_lobby == null && !string.IsNullOrEmpty(m_lobby.Id))
        {
            // Load lobby scene
            SceneManager.LoadSceneAsync("Lobby");
            return;
        }

        // Set video end event handlers
        m_winVideo.loopPointReached += VideoLoopPointReached;
        m_loseVideo.loopPointReached += VideoLoopPointReached;

        // Register vote/retry changed event handlers
        foreach (LobbyUser x in CloudManager.OtherUsers(m_lobby, m_user))
        {
            x.Vote.ValueChanged += OnVoteChanged;
            x.Retry.ValueChanged += OnRetryChanged;
        }
        m_lobby.Retry.ValueChanged += OnLobbyRetryChanged;

        // Trigger vote changed event handler for current user
        OnVoteChanged(CloudManager.OnlyUser(m_lobby, m_user).Vote);
    }

    public async void RetryButtonPressed()
    {
        // Hide retry button
        m_retryButton.enabled = false;
        m_retryButton.image.enabled = false;
        foreach (Transform t in m_retryButton.gameObject.transform)
            t.gameObject.SetActive(false);

        // Show wait text
        m_winOrLoseText.gameObject.SetActive(false);
        m_waitText.gameObject.SetActive(true);

        // Get database objects
        User user; try { user = await User.Get(); } catch { SceneManager.LoadScene("SignIn"); return; }
        Lobby lobby = await Lobby.Get(user);
        LobbyUser lobbyUser = CloudManager.OnlyUser(lobby, user);

        // Set lobby user retry value
        lobbyUser.Retry.Value = true;

        // Trigger retry changed event
        OnRetryChanged(lobbyUser.Retry);
    }

    public async void ResetButtonPressed()
    {
        // Get database objects
        User m_user = await User.Get(); if (m_user == null) { SceneManager.LoadSceneAsync("SignIn"); return; }
        Lobby m_lobby = await Lobby.Get(m_user);

        // Deregister vote/retry changed event handlers
        foreach (LobbyUser x in CloudManager.OtherUsers(m_lobby, m_user))
        {
            x.Vote.ValueChanged -= OnVoteChanged;
            x.Retry.ValueChanged -= OnRetryChanged;
        }
        m_lobby.Retry.ValueChanged -= OnLobbyRetryChanged;

        // Remove user from lobby
        CloudManager.LeaveLobby(m_user, m_lobby);

        // Load lobby scene
        SceneManager.LoadSceneAsync("Lobby");
    }

    void VideoLoopPointReached(VideoPlayer source)
    {
        // Enable win/lose text
        m_winOrLoseText.gameObject.SetActive(true);

        // Enable win/lose button
        foreach (var x in m_winOrLoseButtons)
            x.gameObject.SetActive(true);
    }

    async void OnVoteChanged(CloudNode entry)
    {
        if (entry.Value != null && !string.IsNullOrEmpty(entry.Value))
        {
            // Get database objects
            User user = await User.Get(); if (user == null) { SceneManager.LoadSceneAsync("SignIn"); return; }
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

                        // Set win/lose text and buttons
                        m_winOrLoseText = m_cases[caseNb - 1].WinText;
                        m_winOrLoseButtons.Add(m_resetButton);
                    }
                    else
                    {
                        // Hide wait text
                        m_waitText.gameObject.SetActive(false);

                        // Show lose video
                        m_loseVideo.gameObject.SetActive(true);

                        // Set win/lose text and buttons
                        m_winOrLoseText = m_cases[caseNb - 1].LoseText;
                        m_winOrLoseButtons.Add(m_resetButton);
                        m_winOrLoseButtons.Add(m_retryButton);
                    }
                }
            }
        }
    }

    async void OnRetryChanged(CloudNode<bool> entry)
    {
        // Get database objects
        User user; try { user = await User.Get(); } catch { SceneManager.LoadScene("SignIn"); return; }
        Lobby lobby = await Lobby.Get(user);

        // Check if we are the lobby creator and everyone wants to retry
        if (CloudManager.OnlyUser(lobby, user).Scene.Value == 1 && CloudManager.AllUsers(lobby).All(x => x.Retry.Value.HasValue && x.Ready.Value.Value == true))
        {
            // Attempt to create unique lobby code
            string newCode = await CloudManager.CreateLobbyCode();

            // Create new lobby
            Lobby newLobby = Cloud.Create<Lobby>(new Key("lobbies").Child(newCode));
            newLobby.State.Value = (long)LobbyState.RetryLobby;
            newLobby.Case.Value = lobby.Case.Value;

            // Store the new code in the database
            lobby.Retry.Value = newCode;
            lobby.Case.Value = null;
            lobby.State.Value = null;
            OnLobbyRetryChanged(lobby.Retry);

            /*
            // Store current case
            int caseNb = (int)lobby.Case.Value;

            // Create dictionary to store user's current scenes
            Dictionary<string, int> scenes = new Dictionary<string, int>();

            foreach (LobbyUser x in CloudManager.OtherUsers(lobby, user))
            {
                // Deregister vote/retry changed event handlers
                x.Vote.ValueChanged -= OnVoteChanged;
                x.Retry.ValueChanged -= OnRetryChanged;

                // Get user
                User u = await Cloud.Fetch<User>(new Key("users").Child(x.UserId.Value));

                // Store user's current scene
                scenes[x.UserId.Value] = (int)x.Scene.Value;

                // Remove user from lobby
                CloudManager.LeaveLobby(u, lobby);
            }

            // Leave lobby
            CloudManager.LeaveLobby(user, lobby);

            // Attempt to create unique lobby code
            string newCode = await CloudManager.CreateLobbyCode();

            if (!string.IsNullOrEmpty(newCode))
            {
                // Create new lobby
                lobby = Lobby.Create(newCode);
                lobby.State.Value = (int)LobbyState.Lobby;
                lobby.Case.Value = caseNb;

                // Join lobby
                bool joinSuccess = CloudManager.JoinLobby(user, lobby, LobbyScene.MaxPlayers);
                if (joinSuccess)
                {
                    // Assign scene
                    CloudManager.OnlyUser(lobby, user).Scene.Value = (scenes[user.Id] + 1 > LobbyScene.ScenesPerCase ? scenes[user.Id] + 1 : 1);

                    // Clear static data
                    StaticInventory.Hints.Clear();

                    foreach (var x in scenes)
                    {
                        // Get user
                        User u = await Cloud.Fetch<User>(new Key("users").Child(x.Key));

                        // Add user to lobby
                        bool success = CloudManager.JoinLobby(u, lobby, LobbyScene.MaxPlayers);
                        if (success)
                        {
                            // Assign scene to user
                            CloudManager.OnlyUser(lobby, u).Scene.Value = (scenes[u.Id] + 1 > LobbyScene.ScenesPerCase ? scenes[u.Id] + 1 : 1);
                        }
                    }

                    // Set lobby state value
                    lobby.State.Value = (int)LobbyState.InGame;
                    OnLobbyStateChanged(lobby.State);
                }
            }
            */
        }
    }

    async void OnLobbyRetryChanged(CloudNode retry)
    {
        // Get database objects
        User user; try { user = await User.Get(); } catch { SceneManager.LoadScene("SignIn"); return; }
        Lobby lobby = await Lobby.Get(user);

        // Deregister vote/retry changed event handlers
        foreach (LobbyUser x in CloudManager.OtherUsers(lobby, user))
        {
            x.Vote.ValueChanged -= OnVoteChanged;
            x.Retry.ValueChanged -= OnRetryChanged;
        }
        lobby.Retry.ValueChanged -= OnLobbyRetryChanged;

        // Store new lobby code
        string newLobby = lobby.Retry.Value;

        // Store scene number
        int prevScene = (int)CloudManager.OnlyUser(lobby, user).Scene.Value;

        // Leave lobby
        CloudManager.LeaveLobby(user, lobby);

        // Reset static data
        StaticClues.Reset();
        StaticInventory.Reset();
        StaticRoom.Reset();
        StaticSlot.Reset();
        StaticSuspects.Reset();

        // Fetch new lobby
        user.Lobby.Value = newLobby;
        lobby = await Lobby.Get(user);

        // Join new lobby
        CloudManager.JoinLobby(user, lobby, LobbyScene.MaxPlayers);

        // Calculate new scene number
        int newScene = prevScene + 1;
        if (newScene > LobbyScene.ScenesPerCase)
            newScene = -1;

        // Store new scene number in database
        CloudManager.OnlyUser(lobby, user).Scene.Value = newScene;

        // Load the retry scene
        SceneManager.LoadScene("Retry");
    }

    /*
    async void OnLobbyStateChanged(CloudNode<long> state)
    {
        if (state.Value.HasValue && (LobbyState)state.Value.Value == LobbyState.InGame)
        {
            // Get database objects
            User user; try { user = await User.Get(); } catch { SceneManager.LoadScene("SignIn"); return; }
            Lobby lobby = await Lobby.Get(user);

            // Get lobby case number
            int caseNb = (int)(lobby.Case.Value ?? 0);

            if (caseNb >= 1 && caseNb <= 2)
            {
                // Get this user's scene
                int scene = (int)(CloudManager.OnlyUser(lobby, user).Scene.Value ?? 0);

                if (scene >= 1 && scene <= LobbyScene.ScenesPerCase)
                {
                    // Deregister callbacks

                    // Load this user's scene
                    SceneManager.LoadScene(((caseNb - 1) * LobbyScene.ScenesPerCase) + scene);
                }
            }
        }
    }
    */
}
