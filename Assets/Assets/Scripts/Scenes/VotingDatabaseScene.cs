using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class StaticVotingDatabase
{
    public static bool SeenWelcome = false;
    public static bool HighlightedItem = false;

    public static void Reset()
    {
        SeenWelcome = false;
        HighlightedItem = false;
    }
}

public class VotingDatabaseScene : MonoBehaviour
{
    public GameObject MainScreen, WaitScreen;

    [SerializeField] ModalDialog m_welcomeScreen = null;

    [SerializeField] ModalDialog m_highlightConfirmScreen = null;
    [SerializeField] GameObject ButtonTemplate = null;
    [SerializeField] GameObject[] Backgrounds = new GameObject[4];
    [SerializeField] List<Data> Data = new List<Data>();

    string m_lobbyCode;
    int m_scene;

    int playerItemsLoaded = 0;

    async void Start()
    {
        if (!StaticVotingDatabase.SeenWelcome)
        {
            StaticVotingDatabase.SeenWelcome = true;
            m_welcomeScreen.ShowDialog();
        }

        User m_user; try { m_user = await User.Get(); } catch { SceneManager.LoadScene("SignIn"); return; }
        Lobby m_lobby = await Lobby.Get(m_user);
        if (m_lobby == null)
        {
            SceneManager.LoadScene("Lobby");
            return;
        }

        MainScreen.SetActive(false);
        WaitScreen.SetActive(true);

        int scene = (int)(CloudManager.OnlyUser(m_lobby, m_user).Scene.Value ?? 0);
        if (scene > 0)
        {
            m_scene = scene;
            SetBackground();
            string lobby = m_lobby.Id;
            if (!string.IsNullOrEmpty(lobby))
            {
                m_lobbyCode = lobby;
                await RegisterListeners();
                await DownloadItems();
            }
            else SceneManager.LoadScene("Lobby");
        }
        else SceneManager.LoadScene("Lobby");

        for (int i = 0; i < Data.Count; i++)
        {
            Data data = Data[i];
            data.PlayerButton.GetComponent<Button>().onClick.AddListener(() => PlayerButtonPressed(data));
        }

        PlayerButtonPressed(Data[0]);
    }

    async Task RegisterListeners()
    {
        User user; try { user = await User.Get(); } catch { SceneManager.LoadScene("SignIn"); return; }
        Lobby lobby = await Lobby.Get(user);

        foreach (LobbyUserItem clue in lobby.Users
            .Select(u => u.Items)
            .SelectMany(i => i))
        {
            clue.ValueChanged += OnSlotChanged;
            clue.Highlight.ValueChanged += ItemHighlightValueChanged;
        }
    }

    async Task DeregisterListeners()
    {
        User user; try { user = await User.Get(); } catch { SceneManager.LoadScene("SignIn"); return; }
        Lobby lobby = await Lobby.Get(user);

        foreach (LobbyUserItem clue in lobby.Users
            .Select(u => u.Items)
            .SelectMany(i => i))
        {
            clue.ValueChanged -= OnSlotChanged;
            clue.Highlight.ValueChanged -= ItemHighlightValueChanged;
        }
    }

    async void SetBackground()
    {
        User user; try { user = await User.Get(); } catch { SceneManager.LoadScene("SignIn"); return; }
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

    public async void VotingButtonPressed()
    {
        await DeregisterListeners();
        SceneManager.LoadScene("Voting");
    }

    Data m_current = null;

    private void PlayerButtonPressed(Data data)
    {
        foreach (var button in Data.Select(d => d.PlayerButton))
        {
            ColorBlock colours = button.GetComponent<Button>().colors;
            colours.normalColor = colours.highlightedColor = Color.white;
            button.GetComponent<Button>().colors = colours;
        }
        ColorBlock colours2 = data.PlayerButton.GetComponent<Button>().colors;
        colours2.normalColor = colours2.highlightedColor = Color.green;
        data.PlayerButton.GetComponent<Button>().colors = colours2;

        foreach (var cluePanel in Data.Select(d => d.CluePanel))
        {
            cluePanel.SetActive(false);
        }
        data.CluePanel.SetActive(true);

        for (int slot = 0; slot < data.Slots.Count; ++slot)
        {
            StaticClues.SeenSlots.Add(new SlotData(
                Data.FindIndex(d => d == data).ToString(),
                (slot + 1).ToString(),
                data.Slots[slot].GetComponent<Slot>().Text.GetComponent<Text>().text,
                data.Slots[slot]));
        }

        m_current = data;
    }

    public void PageChanged(GameObject oldPage, GameObject newPage)
    {
        /*Data oldData = Data.First(d => d.CluePanel == oldPage.transform.parent.gameObject);
        if (oldData != null)
        {
            for (int slot = 0; slot < oldData.Slots.Count; ++slot)
            {
                foreach (Transform t in oldData.Slots[slot].transform)
                {
                    foreach (Transform t2 in t)
                    {
                        if (t2.gameObject.name == "Alert")
                            t2.gameObject.SetActive(false);
                    }
                }
            }
        }*/

        Data newData = Data.First(d => d.CluePanel == newPage.transform.parent.gameObject);
        if (newData != null)
        {
            for (int slot = 0; slot < newData.Slots.Count; ++slot)
            {
                StaticClues.SeenSlots.Add(new SlotData(
                    Data.FindIndex(d => d == newData).ToString(),
                    (slot + 1).ToString(),
                    newData.Slots[slot].GetComponent<Slot>().Text.GetComponent<Text>().text,
                    newData.Slots[slot]));
            }
        }
    }

    async Task DownloadItems()
    {
        User m_user; try { m_user = await User.Get(); } catch { SceneManager.LoadScene("SignIn"); return; }
        Lobby m_lobby = await Lobby.Get(m_user);

        for (int i = 0; i < Data.Count; i++)
        {
            foreach (Transform t in Data[i].PlayerButton.transform)
            {
                if (t.gameObject.GetComponent<Text>() != null)
                {
                    if (CloudManager.AllUsers(m_lobby).Count() > i)
                    {
                        string name = CloudManager.AllUsers(m_lobby).ElementAt(i).Name.Value;
                        if (name.Length > 8) name = name.Split(' ')[0];
                        if (name.Length > 8) name = name.Substring(0, 8) + "…";
                        t.gameObject.GetComponent<Text>().text = name;
                    }
                    else
                        t.gameObject.GetComponent<Text>().text = "";
                }
            }
        }

        foreach (LobbyUserItem item in CloudManager.AllUsers(m_lobby).Select(x => x.Items).SelectMany(x => x))
        {
            await OnSlotChangedAsync(item.Name);
            await OnSlotChangedAsync(item.Description);
            await OnSlotChangedAsync(item.Image);
            await ItemHighlightValueChangedAsync(item.Highlight);
        }
    }

    async void OnSlotChanged(CloudNode entry)
    {
        await OnSlotChangedAsync(entry);
    }

    async Task OnSlotChangedAsync(CloudNode entry)
    {
        // Note: entry.Key is lobbies/<lobby_id>/users/<user_id>/items/<item_id>/<field>

        int slotNb = -1;
        if (int.TryParse(entry.Key.Parent.Id, out slotNb))
        {
            User m_user; try { m_user = await User.Get(); } catch { SceneManager.LoadScene("SignIn"); return; }
            Lobby m_lobby = await Lobby.Get(m_user);

            string player = m_lobby.Users.First(x => x.Id == entry.Key.Parent.Parent.Parent.Id).UserId.Value;
            int playerNb = CloudManager.GetPlayerNumber(m_user, m_lobby, player);

            GameObject slot = Data[playerNb].Slots[slotNb];

            if (!string.IsNullOrWhiteSpace(entry.Value))
            {
                if (entry.Key.Id == "name")
                {
                    foreach (Transform t in slot.transform)
                        if (t.gameObject.name == entry.Value)
                            Destroy(t.gameObject);

                    GameObject newObj = Instantiate(ButtonTemplate, ButtonTemplate.transform.parent);
                    newObj.SetActive(true);
                    newObj.name = entry.Value;
                    newObj.transform.SetParent(slot.transform);

                    foreach (Transform t in newObj.transform)
                    {
                        if (t.gameObject.GetComponent<Text>() != null)
                            t.gameObject.GetComponent<Text>().text = entry.Value;
                    }

                    newObj.GetComponent<DragHandler>().enabled = false;

                    newObj.GetComponent<Button>().onClick.AddListener(() => {
                        if (!StaticVotingDatabase.HighlightedItem)
                        {
                            m_highlightConfirmScreen.OnConfirm.RemoveAllListeners();
                            m_highlightConfirmScreen.OnConfirm.AddListener(async () => {
                                StaticVotingDatabase.HighlightedItem = true;
                                CloudNode<bool> highlight = await CloudNode<bool>.Fetch(entry.Key.Parent.Child("highlight"));
                                highlight.Value = true;
                                ItemHighlightValueChanged(highlight);
                            });
                            m_highlightConfirmScreen.ShowDialog();
                        }
                    });
                }
                else if (entry.Key.Id == "description")
                {
                    slot.GetComponent<Slot>().Text.GetComponent<Text>().text = entry.Value;
                }
                else if (entry.Key.Id == "image")
                {
                    foreach (Transform t1 in slot.transform)
                    {
                        foreach (Transform t in t1)
                        {
                            if (t.gameObject.name == "Image" && t.gameObject.GetComponent<Image>() != null)
                                t.gameObject.GetComponent<Image>().sprite = Resources.Load<Sprite>(entry.Value);

                            if (t.gameObject.GetComponent<Text>() != null)
                                t.gameObject.GetComponent<Text>().text = "";
                        }
                    }
                }
            }
            else
            {
                if (entry.Key.Id == "image")
                {
                    foreach (Transform t1 in slot.transform)
                    {
                        foreach (Transform t in t1)
                        {
                            if (t.gameObject.name == "Image" && t.gameObject.GetComponent<Image>() != null)
                                t.gameObject.SetActive(false);
                        }
                    }
                }
                else
                {
                    slot.GetComponent<Slot>().Text.GetComponent<Text>().text = "";

                    foreach (Transform t1 in slot.transform)
                        Destroy(t1.gameObject);
                }
            }

            if (player == m_user.Id)
                CheckPlayerItemsLoaded();
        }
    }

    async void ItemHighlightValueChanged(CloudNode<bool> entry)
    {
        await ItemHighlightValueChangedAsync(entry);
    }

    async Task ItemHighlightValueChangedAsync(CloudNode<bool> entry)
    {
        // Note: entry.Key is lobbies/<lobby_id>/users/<user_id>/items/<item_id>/highlight

        int slotNb = -1;
        if (int.TryParse(entry.Key.Parent.Id, out slotNb))
        {
            User user; try { user = await User.Get(); } catch { SceneManager.LoadScene("SignIn"); return; }
            Lobby lobby = await Lobby.Get(user);

            string player = lobby.Users.First(x => x.Id == entry.Key.Parent.Parent.Parent.Id).UserId.Value;
            int playerNb = CloudManager.GetPlayerNumber(user, lobby, player);

            GameObject slot = Data[playerNb].Slots[slotNb];

            bool isHighlighted = (entry.Value.HasValue && entry.Value.Value);

            foreach (Transform t in slot.transform)
                foreach (Transform t2 in t)
                    if (t2.gameObject.name == "Alert")
                        t2.gameObject.SetActive(isHighlighted);

            foreach (Transform t3 in Data[playerNb].PlayerButton.transform)
                if (t3.gameObject.name == "Alert")
                    t3.gameObject.SetActive(isHighlighted);
        }
    }

    private void CheckPlayerItemsLoaded()
    {
        playerItemsLoaded++;

        if (playerItemsLoaded >= 18)
        {
            WaitScreen.SetActive(false);
            MainScreen.SetActive(true);
        }
    }
}
