using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
public class Data
{
    [SerializeField] public GameObject PlayerButton;
    [SerializeField] public GameObject CluePanel;
    [SerializeField] public List<GameObject> Slots;
}

public struct SlotData : IEquatable<SlotData>
{
    public string Player;
    public string Slot;
    public string Name;
    public GameObject Object;

    public SlotData(string player, string slot, string name, GameObject obj = null)
    {
        Player = player;
        Slot = slot;
        Name = name;
        Object = obj;
    }

    public bool Equals(SlotData other)
    {
        return Player == other.Player && Slot == other.Slot && Name == other.Name;
    }
}

public static class StaticClues
{
    public static List<SlotData> SeenSlots = new List<SlotData>();

    public static void Reset()
    {
        SeenSlots.Clear();
    }
}

public class DatabaseScene : MonoBehaviour
{
    public GameObject MainScreen, WaitScreen;

    [SerializeField] EditItemText EditScreen = null;
    [SerializeField] GameObject ReadyButton = null;
    [SerializeField] GameObject ReturnButton = null;
    [SerializeField] GameObject ButtonTemplate = null;
    [SerializeField] GameObject[] Backgrounds = new GameObject[4];
    [SerializeField] List<Data> Data = new List<Data>();

    string m_lobbyCode;
    int m_scene;

    int playerItemsLoaded = 0;

    async void Start()
    {
        User m_user = await User.Get();
        Lobby m_lobby = await Lobby.Get(m_user);

        if (m_lobby == null)
        {
            SceneManager.LoadScene("Lobby");
            return;
        }

        MainScreen.SetActive(false);
        WaitScreen.SetActive(true);

        int scene = (int)(m_lobby.Users.First(u => u.UserId.Value == m_user.Id).Scene.Value ?? 0);
        if (scene > 0)
        {
            m_scene = scene;
            SetBackground();
            string lobby = m_lobby.Id;
            if (!string.IsNullOrEmpty(lobby))
            {
                m_lobbyCode = lobby;
                await DownloadItems();
                await RegisterListeners();
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
        User m_user = await User.Get();
        Lobby m_lobby = await Lobby.Get(m_user);

        foreach (LobbyUserItem clue in m_lobby.Users.Where(u => u.UserId.Value != m_user.Id).Select(u => u.Items).SelectMany(i => i))
            clue.ValueChanged += OnSlotChangedAsync;

        foreach (LobbyUser user in m_lobby.Users)
            user.Ready.ValueChanged += OnReadyChanged;
    }

    void SetBackground()
    {
        if (m_scene <= Backgrounds.Length)
        {
            foreach (var bg in Backgrounds)
                bg.SetActive(false);

            Backgrounds[m_scene - 1].SetActive(true);
        }
    }

    public async void ConfirmReady()
    {
        if (ReadyButton.activeSelf)
        {
            ReadyButton.SetActive(false);
            User m_user = await User.Get();
            Lobby m_lobby = await Lobby.Get(m_user);
            ReadyButton.SetActive(true);
            ReadyButton.GetComponent<Image>().color = Color.yellow;
            foreach (Transform t in ReadyButton.gameObject.transform)
            {
                var text = t.GetComponent<Text>();
                if (text != null) text.text = "Waiting...";
            }
            CloudNode<bool> ready = m_lobby.Users.First(u => u.UserId.Value == m_user.Id).Ready;
            ready.Value = true;
            OnReadyChanged(ready);
        }
    }

    public async void ReturnButtonPressed()
    {
        if (ReturnButton.activeSelf)
        {
            ReturnButton.SetActive(false);
            await DeregisterListeners();
            SceneManager.LoadScene(m_scene);
        }
    }

    async Task DeregisterListeners()
    {
        User m_user = await User.Get();
        Lobby m_lobby = await Lobby.Get(m_user);

        foreach (LobbyUserItem clue in m_lobby.Users.Where(u => u.UserId.Value != m_user.Id).Select(u => u.Items).SelectMany(i => i))
            clue.ValueChanged -= OnSlotChangedAsync;

        foreach (LobbyUser user in m_lobby.Users)
            user.Ready.ValueChanged -= OnReadyChanged;
    }

    Data m_current = null;

    void PlayerButtonPressed(Data data)
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

        if (m_current != null)
        {
            for (int slot = 0; slot < m_current.Slots.Count; ++slot)
            {
                foreach (Transform t in m_current.Slots[slot].transform)
                {
                    foreach (Transform t2 in t)
                    {
                        if (t2.gameObject.name == "Alert")
                            t2.gameObject.SetActive(false);
                    }
                }
            }

            foreach (Transform t2 in m_current.PlayerButton.transform)
            {
                if (t2.gameObject.name == "Alert")
                    t2.gameObject.SetActive(false);
            }
        }

        for (int slot = 0; slot < data.Slots.Count; ++slot)
        {
            foreach (Transform t in data.Slots[slot].transform)
            {
                //Debug.Log("BTNPRS = player-" + Data.FindIndex(d => d == data) + "/slot-" + (slot + 1) + " = " + t.gameObject.name);
                StaticClues.SeenSlots.Add(new SlotData(Data.FindIndex(d => d == data).ToString(), (slot + 1).ToString(), t.gameObject.name, data.Slots[slot]));
            }
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
                foreach (Transform t in newData.Slots[slot].transform)
                {
                    //Debug.Log("BTNPRS = player-" + Data.FindIndex(d => d == data) + "/slot-" + (slot + 1) + " = " + t.gameObject.name);
                    StaticClues.SeenSlots.Add(new SlotData(Data.FindIndex(d => d == newData).ToString(), (slot + 1).ToString(), t.gameObject.name, newData.Slots[slot]));
                }
            }
        }
    }

    public async Task UploadItem(int slot, ObjectHintData hint)
    {
        User m_user = await User.Get();
        Lobby m_lobby = await Lobby.Get(m_user);

        CloudManager.UploadDatabaseItem(m_user, m_lobby, slot, hint);
    }

    public async Task RemoveItem(int slot)
    {
        //if (!m_readyPlayers.Any(p => p.Value == false))
        //{
        User m_user = await User.Get();
        Lobby m_lobby = await Lobby.Get(m_user);

        CloudManager.RemoveDatabaseItem(m_user, m_lobby, slot);
        //}
    }

    async Task DownloadItems()
    {
        User m_user = await User.Get();
        Lobby m_lobby = await Lobby.Get(m_user);

        int tmp = 0;
        //User player = await CloudManager.DownloadClues(tmp);
        for (int j = 0; j < m_lobby.Users.First(u => u.UserId.Value == m_user.Id).Items.Length; j++)
        {
            int tmp2 = j;
            LobbyUserItem clue = m_lobby.Users.First(u => u.UserId.Value == m_user.Id).Items[tmp2];
            CheckPlayerItemsLoaded();
            if (!string.IsNullOrEmpty(clue.Name.Value))
            {
                var slot = Data[tmp].Slots[tmp2];
                foreach (Transform t in slot.transform) if (t.gameObject.name == clue.Name.Value) Destroy(t.gameObject);
                var newObj = Instantiate(ButtonTemplate, ButtonTemplate.transform.parent);
                newObj.SetActive(true);
                newObj.name = clue.Name.Value;
                newObj.transform.SetParent(slot.transform);
                if (!string.IsNullOrEmpty(clue.Image.Value))
                {
                    foreach (Transform t in newObj.transform)
                    {
                        if (t.gameObject.GetComponent<Text>() != null)
                        {
                            t.gameObject.GetComponent<Text>().text = clue.Name.Value;
                        }
                        if (t.gameObject.GetComponent<Image>() != null)
                        {
                            t.gameObject.GetComponent<Image>().sprite = Resources.Load<Sprite>(clue.Image.Value);
                        }
                    }
                }
                else
                {
                    foreach (Transform t in newObj.transform)
                    {
                        if (t.gameObject.GetComponent<Text>() != null)
                        {
                            t.gameObject.GetComponent<Text>().text = clue.Name.Value;
                            t.gameObject.GetComponent<Text>().gameObject.SetActive(true);
                        }
                        if (t.gameObject.GetComponent<Image>() != null)
                        {
                            t.gameObject.GetComponent<Image>().gameObject.SetActive(false);
                        }
                    }
                }
                newObj.GetComponent<DragHandler>().enabled = false;
                newObj.GetComponent<Button>().onClick.AddListener(async () => {
                    if (StaticSlot.TimesRemoved < StaticSlot.MaxRemovals)
                    {
                        slot.GetComponent<Slot>().Text.GetComponent<Text>().text = "";
                        slot.GetComponent<Slot>().EditButton.gameObject.SetActive(false);
                        slot.GetComponent<Slot>().EditButton.onClick.RemoveAllListeners();
                        await RemoveItem(slot.GetComponent<Slot>().SlotNumber);
                        Destroy(newObj);
                        StaticSlot.TimesRemoved++;
                    }
                    else Debug.Log("YOU CANT GO THERE (EG. you have removed your maximum amount of times)");
                });
                slot.GetComponent<Slot>().Text.GetComponent<Text>().text = clue.Description.Value;
                slot.GetComponent<Slot>().EditButton.gameObject.SetActive(true);
                slot.GetComponent<Slot>().EditButton.onClick.AddListener(() => {
                    MainScreen.SetActive(false);
                    EditScreen.gameObject.SetActive(true);
                    EditScreen.SetTextField(clue.Description.Value);
                    EditScreen.OnCancel = () => {
                        EditScreen.OnCancel = null;
                        EditScreen.OnSubmit = null;
                        EditScreen.gameObject.SetActive(false);
                        MainScreen.SetActive(true);
                    };
                    EditScreen.OnSubmit = newText => {
                        EditScreen.OnCancel = null;
                        EditScreen.OnSubmit = null;
                        EditScreen.gameObject.SetActive(false);
                        MainScreen.SetActive(true);
                        clue.Description.Value = newText;
                        slot.GetComponent<Slot>().Text.GetComponent<Text>().text = clue.Description.Value;
                    };
                });
            }
        }
    }

    async void OnSlotChangedAsync(CloudNode entry)
    {
        if (ReadyButton == null)
            return;

        //Debug.Log(entry.Key + " | " + (args.Snapshot.Exists ? args.Snapshot.Value.ToString() : ""));
        User m_user = await User.Get();
        Lobby m_lobby = await Lobby.Get(m_user);

        string player = m_lobby.Users.First(x => x.Id == entry.Key.Parent.Parent.Parent.Id).UserId.Value;
        string field = entry.Key.Id;

        if (entry.Value != null)
        {
            string value = entry.Value;

            int slotNb = -1;
            if (!string.IsNullOrEmpty(value) && int.TryParse(entry.Key.Parent.Id, out slotNb))
            {

                int playerNb = CloudManager.GetPlayerNumber(m_user, m_lobby, player);
                var slot = Data[playerNb].Slots[slotNb - 1];
                if (field == "name")
                {
                    foreach (Transform t in slot.transform) if (t.gameObject.name == value) Destroy(t.gameObject);
                    var newObj = Instantiate(ButtonTemplate, ButtonTemplate.transform.parent);
                    newObj.SetActive(true);
                    newObj.name = value;
                    newObj.transform.SetParent(slot.transform);
                    foreach (Transform t in newObj.transform)
                    {
                        if (t.gameObject.GetComponent<Text>() != null)
                        {
                            t.gameObject.GetComponent<Text>().text = value;
                        }
                        //Debug.Log(string.Format("LOAD = player-{0}/slot-{1} = {2}", playerNb.ToString(), slotNb.ToString(), value));
                        if (t.gameObject.name == "Alert" && !StaticClues.SeenSlots.Any(s => s.Equals(new SlotData(playerNb.ToString(), slotNb.ToString(), value))))
                        {
                            t.gameObject.SetActive(true);
                            foreach (Transform t2 in Data[playerNb].PlayerButton.transform)
                            {
                                if (t2.gameObject.name == "Alert")
                                    t2.gameObject.SetActive(true);
                            }
                        }
                    }
                    newObj.GetComponent<DragHandler>().enabled = false;
                    //CheckItemsLoaded();
                }
                else if (field == "hint")
                {
                    slot.GetComponent<Slot>().Text.GetComponent<Text>().text = value;
                }
                else if (field == "image")
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        foreach (Transform t1 in slot.transform)
                        {
                            foreach (Transform t in t1)
                            {
                                if (t.gameObject.name == "Image" && t.gameObject.GetComponent<Image>() != null)
                                {
                                    t.gameObject.GetComponent<Image>().sprite = Resources.Load<Sprite>(value);
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (Transform t1 in slot.transform)
                        {
                            foreach (Transform t in t1)
                            {
                                if (t.gameObject.name == "Image" && t.gameObject.GetComponent<Image>() != null)
                                {
                                    t.gameObject.SetActive(false);
                                }
                                if (t.gameObject.GetComponent<Text>() != null)
                                {
                                    t.gameObject.SetActive(true); // TODO: REMOVE TEMP FIX
                                }
                            }
                        }
                    }
                }
            }
        }
        else
        {
            int slotNb = -1;
            if (int.TryParse(entry.Key.Parent.Id, out slotNb))
            {
                int playerNb = CloudManager.GetPlayerNumber(m_user, m_lobby, player);
                var slot = Data[playerNb].Slots[slotNb - 1];

                slot.GetComponent<Slot>().Text.GetComponent<Text>().text = "";

                foreach (Transform t1 in slot.transform)
                {
                    Destroy(t1.gameObject);
                }
            }
        }
    }

    void CheckPlayerItemsLoaded()
    {
        playerItemsLoaded++;

        //if (playerItemsLoaded >= 24)
        {
            WaitScreen.SetActive(false);
            MainScreen.SetActive(true);
        }
    }

    async void OnReadyChanged(CloudNode<bool> entry)
    {
        if (ReadyButton == null)
            return;

        if (entry.Value != null)
        {
            bool value = entry.Value ?? false;

            if (value == true)
            {
                User m_user = await User.Get();
                Lobby m_lobby = await Lobby.Get(m_user);

                bool everyoneReady = CloudManager.AllUsers(m_lobby).All(x => x.Ready.Value.HasValue && x.Ready.Value == true);

                if (everyoneReady)
                {
                    SceneManager.LoadScene("Voting");
                }
            }
        }
    }
}
