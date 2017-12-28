using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class StaticSuspects
{
    public static List<VotingSuspectData> DiscardedSuspects = new List<VotingSuspectData>();

    public static void Reset()
    {
        DiscardedSuspects.Clear();
    }
}

public class VotingController : MonoBehaviour
{
    [SerializeField] private GameObject ResetButton = null;
    [SerializeField] private GameObject ReturnButton = null;
    //[SerializeField] private GameObject VoteButton = null;
    [SerializeField] private GameObject[] Backgrounds = new GameObject[4];
    [SerializeField] private VotingSuspect[] Suspects = new VotingSuspect[8];

    private OnlineManager NetworkController;
    private string m_lobby;
    private int m_scene;

    private void Start()
    {
        NetworkController = new OnlineManager();

        ResetButton.SetActive(false);
        ReturnButton.SetActive(false);
        //VoteButton.SetActive(false);

        NetworkController.GetPlayerScene(scene => {
            if (scene > 0)
            {
                m_scene = scene;
                SetBackground();
                NetworkController.GetPlayerLobby(lobby => {
                    if (!string.IsNullOrEmpty(lobby)) {
                        m_lobby = lobby;
                        ResetButton.SetActive(true);
                        ReturnButton.SetActive(true);
                        //VoteButton.SetActive(true);
                        DiscardSuspects();
                    }
                    else SceneManager.LoadScene("Communication Detective/Scenes/Lobby");
                });
            }
            else SceneManager.LoadScene("Communication Detective/Scenes/Lobby");
        });
    }

    private void DiscardSuspects()
    {
        foreach (var discarded in StaticSuspects.DiscardedSuspects)
        {
            foreach (var suspect in Suspects)
            {
                if (suspect.Name.text == discarded.Name)
                {
                    var page = suspect.GetComponent<VotingPageController>();

                    var prevPage = page.PanelLeft;
                    var nextPage = page.PanelRight;

                    prevPage.GetComponent<VotingPageController>().PanelRight = nextPage;
                    nextPage.GetComponent<VotingPageController>().PanelLeft = prevPage;

                    // TODO: don't remove last item

                    page.Right();
                    suspect.gameObject.SetActive(false);
                }
            }
        }
    }

    private void SetBackground()
    {
        if (m_scene <= Backgrounds.Length)
        {
            foreach (var bg in Backgrounds)
                bg.SetActive(false);

            Backgrounds[m_scene - 1].SetActive(true);
        }
    }

    public void ReturnButtonPressed()
    {
        if (ReturnButton.activeSelf)
        {
            ReturnButton.SetActive(false);
            SceneManager.LoadScene("Communication Detective/Scenes/VotingDatabase");
        }
    }

    public void ResetButtonPressed()
    {
        if (ResetButton.activeSelf)
        {
            ResetButton.SetActive(false);

            for (int i = 0; i < Suspects.Length; i++)
            {
                int prevIdx = i - 1;
                int nextIdx = i + 1;

                if (prevIdx < 0)
                    prevIdx = Suspects.Length - 1;

                if (nextIdx >= Suspects.Length)
                    nextIdx = 0;

                var current = Suspects[i];

                var prev = Suspects[prevIdx];
                var next = Suspects[nextIdx];

                var page = current.gameObject.GetComponent<VotingPageController>();

                page.PanelLeft = prev.gameObject;
                page.PanelRight = next.gameObject;

                // reset opacity
                if (current.Slot != null)
                {
                    var color = current.Slot.GetComponent<Image>().color;
                    color.a = 1.0f;
                    current.Slot.GetComponent<Image>().color = color;
                }
            }

            ResetButton.SetActive(true);
        }
    }

    public void ConfirmVote()
    {
        var current = Suspects.First(s => s.gameObject.activeSelf);
        if (current != null)
        {
            NetworkController.SubmitVote(current.Name.text, success => {
                SceneManager.LoadScene("Communication Detective/Scenes/VotingWait");
            });
        }
    }
}
