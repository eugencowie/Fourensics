using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

public class VotingScene : MonoBehaviour
{
    [Serializable]
    struct Case
    {
        public GameObject Container;
        public VotingSuspect[] Suspects;
    }

    [SerializeField] private GameObject ResetButton = null;
    [SerializeField] private GameObject ReturnButton = null;
    //[SerializeField] private GameObject VoteButton = null;
    [SerializeField] private GameObject[] Backgrounds = new GameObject[4];
    [SerializeField] private Case[] m_cases = new Case[2];

    private string m_lobbyCode;
    private int m_scene;

    async void Start()
    {
        // Get database objects
        User user; try { user = await User.Get(); } catch { SceneManager.LoadSceneAsync("SignIn"); return; }
        Lobby lobby = await Lobby.Get(user);

        int scene = (int)(CloudManager.OnlyUser(lobby, user).Scene.Value ?? 0);
        int caseNb = (int)(lobby.Case.Value ?? 0);

        if (lobby == null || string.IsNullOrEmpty(lobby.Id) || scene <= 0 || caseNb < 1 || caseNb > 2 || (caseNb - 1) >= m_cases.Length)
        {
            SceneManager.LoadSceneAsync("Lobby");
            return;
        }

        foreach (var x in m_cases) x.Container.SetActive(false);
        m_cases[caseNb - 1].Container.SetActive(true);

        ResetButton.SetActive(false);
        ReturnButton.SetActive(false);
        //VoteButton.SetActive(false);

        m_scene = scene;
        SetBackground();
        m_lobbyCode = lobby.Id;
        ResetButton.SetActive(true);
        ReturnButton.SetActive(true);
        //VoteButton.SetActive(true);
        await DiscardSuspects();
    }

    async Task DiscardSuspects()
    {
        foreach (var discarded in StaticSuspects.DiscardedSuspects)
        {
            // Get database objects
            User user; try { user = await User.Get(); } catch { SceneManager.LoadSceneAsync("SignIn"); return; }
            Lobby lobby = await Lobby.Get(user);

            // Get lobby case number
            int caseNb = (int)(lobby.Case.Value ?? 0);

            if (caseNb >= 1 && caseNb <= 2 && (caseNb - 1) < m_cases.Length)
            {
                foreach (var suspect in m_cases[caseNb - 1].Suspects)
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
    }

    async void SetBackground()
    {
        User user; try { user = await User.Get(); } catch { SceneManager.LoadSceneAsync("SignIn"); return; }
        Lobby lobby = await Lobby.Get(user);

        int caseNb = (int)(lobby.Case.Value ?? 0);
        if (caseNb >= 1 && caseNb <= 2)
        {
            const int scenesPerCase = 4;

            int scene = (int)(CloudManager.OnlyUser(lobby, user).Scene.Value ?? 0);
            if (scene >= 1 && scene <= scenesPerCase)
            {
                int x = ((caseNb - 1) * scenesPerCase) + scene;

                if (x <= Backgrounds.Length)
                {
                    foreach (var bg in Backgrounds)
                        bg.SetActive(false);

                    Backgrounds[x - 1].SetActive(true);
                }
            }
        }
    }

    public void ReturnButtonPressed()
    {
        if (ReturnButton.activeSelf)
        {
            ReturnButton.SetActive(false);
            SceneManager.LoadSceneAsync("VotingDatabase");
        }
    }

    public async void ResetButtonPressed()
    {
        if (ResetButton.activeSelf)
        {
            ResetButton.SetActive(false);

            // Get database objects
            User user; try { user = await User.Get(); } catch { SceneManager.LoadSceneAsync("SignIn"); return; }
            Lobby lobby = await Lobby.Get(user);

            // Get lobby case number
            int caseNb = (int)(lobby.Case.Value ?? 0);

            if (caseNb >= 1 && caseNb <= 2 && (caseNb - 1) < m_cases.Length)
            {
                for (int i = 0; i < m_cases[caseNb - 1].Suspects.Length; i++)
                {
                    int prevIdx = i - 1;
                    int nextIdx = i + 1;

                    if (prevIdx < 0)
                        prevIdx = m_cases[caseNb - 1].Suspects.Length - 1;

                    if (nextIdx >= m_cases[caseNb - 1].Suspects.Length)
                        nextIdx = 0;

                    var current = m_cases[caseNb - 1].Suspects[i];

                    var prev = m_cases[caseNb - 1].Suspects[prevIdx];
                    var next = m_cases[caseNb - 1].Suspects[nextIdx];

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
            }

            ResetButton.SetActive(true);
        }
    }

    public async void ConfirmVote()
    {
        User m_user; try { m_user = await User.Get(); } catch { SceneManager.LoadSceneAsync("SignIn"); return; }
        Lobby m_lobby = await Lobby.Get(m_user);

        // Get database objects
        User user; try { user = await User.Get(); } catch { SceneManager.LoadSceneAsync("SignIn"); return; }
        Lobby lobby = await Lobby.Get(user);

        // Get lobby case number
        int caseNb = (int)(lobby.Case.Value ?? 0);

        if (caseNb >= 1 && caseNb <= 2 && (caseNb - 1) < m_cases.Length)
        {
            var current = m_cases[caseNb - 1].Suspects.First(s => s.gameObject.activeSelf);
            if (current != null)
            {
                CloudManager.OnlyUser(m_lobby, m_user).Vote.Value = current.Name.text;
                SceneManager.LoadSceneAsync("VotingWait");
            }
        }
    }
}
