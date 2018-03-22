using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

    private Text m_winOrLoseText;

    async void Start()
    {
        User m_user; try { m_user = await User.Get(); } catch { SceneManager.LoadScene("SignIn"); return; }
        Lobby m_lobby = await Lobby.Get(m_user);

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
            await RegisterListeners();
            OnVoteChanged(CloudManager.OnlyUser(m_lobby, m_user).Vote);
        }
        else SceneManager.LoadScene("Lobby");
    }

    private async Task RegisterListeners()
    {
        User m_user; try { m_user = await User.Get(); } catch { SceneManager.LoadScene("SignIn"); return; }
        Lobby m_lobby = await Lobby.Get(m_user);

        foreach (LobbyUser user in CloudManager.OtherUsers(m_lobby, m_user))
            user.Vote.ValueChanged += OnVoteChanged;
    }

    private async Task DeregisterListeners()
    {
        User m_user; try { m_user = await User.Get(); } catch { SceneManager.LoadScene("SignIn"); return; }
        Lobby m_lobby = await Lobby.Get(m_user);

        foreach (LobbyUser user in CloudManager.OtherUsers(m_lobby, m_user))
            user.Vote.ValueChanged -= OnVoteChanged;
    }

    private void VideoLoopPointReached(VideoPlayer source)
    {
        //source.gameObject.SetActive(false);
        m_winOrLoseText.gameObject.SetActive(true);
        ResetButton.SetActive(true);
    }

    public async void ResetButtonPressed()
    {
        User m_user; try { m_user = await User.Get(); } catch { SceneManager.LoadScene("SignIn"); return; }
        Lobby m_lobby = await Lobby.Get(m_user);

        await DeregisterListeners();
        CloudManager.LeaveLobby(m_user, m_lobby);
        SceneManager.LoadScene("Lobby");
    }

    private async void OnVoteChanged(CloudNode entry)
    {
        if (entry.Value != null)
        {
            string value = entry.Value;

            if (!string.IsNullOrEmpty(value))
            {
                User m_user; try { m_user = await User.Get(); } catch { SceneManager.LoadScene("SignIn"); return; }
                Lobby m_lobby = await Lobby.Get(m_user);

                bool everyoneVoted = CloudManager.AllUsers(m_lobby).All(x => !string.IsNullOrWhiteSpace(x.Vote.Value));

                if (everyoneVoted)
                {
                    IEnumerable<LobbyUser> allUsers = CloudManager.AllUsers(m_lobby);

                    float correctAnswers = allUsers.Count(x => x.Vote.Value == "Caleb Holden");
                    float totalAnswers = allUsers.Count();

                    float percentage = correctAnswers / totalAnswers;
                    float requiredPercentage = RequiredVotePercentage / 100.0f;

                    if (percentage >= requiredPercentage)
                    {
                        WinText.text += $"\n\nYou voted for {CloudManager.OnlyUser(m_lobby, m_user).Vote.Value}";

                        IEnumerable<LobbyUser> otherUsers = CloudManager.OtherUsers(m_lobby, m_user);

                        int counter = 2;
                        foreach (LobbyUser user in otherUsers)
                        {
                            WinText.text += $"\nPlayer {counter} voted for {user.Vote.Value}";
                            counter++;
                        }

                        WaitText.gameObject.SetActive(false);
                        WinVideo.gameObject.SetActive(true);

                        m_winOrLoseText = WinText;
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
