using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class StaticRoom
{
    public static bool SeenWelcome = false;
    public static Quaternion PrevCameraRotation;

    public static void Reset()
    {
        SeenWelcome = false;
    }
}

public class RoomScene : MonoBehaviour
{
    [SerializeField] private GameObject ReadyButton = null;
    [SerializeField] private GameObject DatabaseButton = null;

    public GameObject mainScreen;
    public GameObject welcomeScreen;

    async void Start()
    {
        if (StaticRoom.SeenWelcome)
        {
            mainScreen.SetActive(true);
            welcomeScreen.SetActive(false);
            Camera.main.transform.localRotation = StaticRoom.PrevCameraRotation;
        }
        StaticRoom.SeenWelcome = true;

        ReadyButton.SetActive(false);
        DatabaseButton.SetActive(false);

        User m_user = await User.Get(); if (m_user == null) { SceneManager.LoadSceneAsync("SignIn"); return; }
        Lobby m_lobby = await Lobby.Get(m_user);
        if (m_lobby == null)
        {
            SceneManager.LoadSceneAsync("Lobby");
            return;
        }

        string room = m_lobby.Id;
        if (!string.IsNullOrEmpty(room))
        {
            await RegisterListeners();
            ReadyButton.SetActive(true);
            DatabaseButton.SetActive(true);
        }
        else SceneManager.LoadSceneAsync("Lobby");
    }

    private async Task RegisterListeners()
    {
        User m_user = await User.Get(); if (m_user == null) { SceneManager.LoadSceneAsync("SignIn"); return; }
        Lobby m_lobby = await Lobby.Get(m_user);

        foreach (LobbyUserItem clue in CloudManager.OtherUsers(m_lobby, m_user).Select(u => u.Items).SelectMany(i => i))
            clue.ValueChanged += OnSlotChanged;

        foreach (LobbyUser user in CloudManager.AllUsers(m_lobby))
            user.Ready.ValueChanged += OnReadyChanged;
    }

    public void DatabaseButtonPressed()
    {
        if (DatabaseButton.activeSelf)
        {
            StaticRoom.PrevCameraRotation = Camera.main.transform.localRotation;
            DatabaseButton.SetActive(false);
            SceneManager.LoadSceneAsync("Database");
        }
    }

    private async void OnReadyChanged(CloudNode<bool> entry)
    {
        Debug.Log($"RoomScene.OnReadyChanged({entry.Value})");

        if (ReadyButton == null)
            return;

        if (entry.Value != null)
        {
            bool value = entry.Value ?? false;

            if (value == true)
            {
                User m_user = await User.Get(); if (m_user == null) { SceneManager.LoadSceneAsync("SignIn"); return; }
                Lobby m_lobby = await Lobby.Get(m_user);

                bool everyoneReady = CloudManager.AllUsers(m_lobby).All(x => x.Ready.Value.HasValue && x.Ready.Value == true);

                if (everyoneReady)
                {
                    StaticClues.SeenSlots.Clear();
                    StaticVotingDatabase.Reset();
                    m_lobby.State.Value = (long)LobbyState.Voting;
                    SceneManager.LoadSceneAsync("VotingDatabase");
                }
            }
        }
    }

    public async void ConfirmLeave()
    {
        User m_user = await User.Get(); if (m_user == null) { SceneManager.LoadSceneAsync("SignIn"); return; }
        Lobby m_lobby = await Lobby.Get(m_user);
        CloudManager.LeaveLobby(m_user, m_lobby);
        SceneManager.LoadSceneAsync("Lobby");

        //NetworkController.LeaveLobby(m_roomCode, success => {
        //    if (success) SceneManager.LoadSceneAsync("Lobby");
        //});
    }

    public async void ConfirmReady()
    {
        if (ReadyButton.activeSelf)
        {
            ReadyButton.SetActive(false);
            User m_user = await User.Get(); if (m_user == null) { SceneManager.LoadSceneAsync("SignIn"); return; }
            Lobby m_lobby = await Lobby.Get(m_user);
            ReadyButton.SetActive(true);
            ReadyButton.GetComponent<Image>().color = Color.yellow;
            foreach (Transform t in ReadyButton.gameObject.transform)
            {
                var text = t.GetComponent<Text>();
                if (text != null) text.text = "Waiting...";
            }
            CloudNode<bool> ready = CloudManager.OnlyUser(m_lobby, m_user).Ready;
            ready.Value = true;
            OnReadyChanged(ready);
        }
    }

    private async void OnSlotChanged(CloudNode entry)
    {
        int slotNb = -1;
        if (int.TryParse(entry.Key.Parent.Id, out slotNb))
        {
            if (entry.Value != null && entry.Key.Id == "description")
            {
                string value = entry.Value;

                User m_user = await User.Get(); if (m_user == null) { SceneManager.LoadSceneAsync("SignIn"); return; }
                Lobby m_lobby = await Lobby.Get(m_user);

                string player = m_lobby.Users.First(x => x.Id == entry.Key.Parent.Parent.Parent.Id).UserId.Value;
                int playerNb = CloudManager.GetPlayerNumber(m_user, m_lobby, player);

                if (DatabaseButton != null && !StaticClues.SeenSlots.Any(s => s.Equals(new SlotData(playerNb.ToString(), (slotNb + 1).ToString(), value))))
                {
                    foreach (Transform t in DatabaseButton.transform)
                    {
                        if (t.gameObject.name == "Alert")
                            t.gameObject.SetActive(true);
                    }
                }
            }
        }
    }
}
