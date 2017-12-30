using Firebase.Database;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class StaticRoom
{
    public static bool SeenWelcome = false;

    public static void Reset()
    {
        SeenWelcome = false;
    }
}

public class RoomController : MonoBehaviour
{
    [SerializeField] private GameObject ReadyButton = null;
    [SerializeField] private GameObject DatabaseButton = null;

    private OnlineManager NetworkController;

    private string m_roomCode;

    private Dictionary<string, bool> m_readyPlayers = new Dictionary<string, bool>();

    public GameObject mainScreen;
    public GameObject welcomeScreen;

    async void Start()
    {
        Debug.Log("Seen welcome? " + StaticRoom.SeenWelcome);
        if (StaticRoom.SeenWelcome)
        {
            mainScreen.SetActive(true);
            welcomeScreen.SetActive(false);
        }
        StaticRoom.SeenWelcome = true;

        NetworkController = new OnlineManager();

        ReadyButton.SetActive(false);
        DatabaseButton.SetActive(false);

        string room = await NetworkController.GetPlayerLobby();
        if (!string.IsNullOrEmpty(room))
        {
            string[] players = await NetworkController.GetPlayers(room);
            m_roomCode = room;
            foreach (var player in players) m_readyPlayers[player] = false;
            NetworkController.RegisterReadyChanged(room, OnReadyChanged);
            NetworkController.RegisterCluesChanged(room, OnSlotChanged);
            ReadyButton.SetActive(true);
            DatabaseButton.SetActive(true);
        }
        else SceneManager.LoadScene("Lobby");
    }

    public void DatabaseButtonPressed()
    {
        if (DatabaseButton.activeSelf)
        {
            DatabaseButton.SetActive(false);
            SceneManager.LoadScene("Database");
        }
    }

    private void OnReadyChanged(OnlineDatabaseEntry entry, ValueChangedEventArgs args)
    {
        if (ReadyButton == null)
            return;

        if (args.Snapshot.Exists)
        {
            string value = args.Snapshot.Value.ToString();

            if (value == "true")
            {
                string[] key = entry.Key.Split('/');
                string player = key[1];
                m_readyPlayers[player] = true;

                if (player == SignIn.GetPlayerId())
                {
                    ConfirmReady();
                }

                if (!m_readyPlayers.Any(p => p.Value == false))
                {
                    NetworkController.DeregisterReadyChanged(m_roomCode);
                    SceneManager.LoadScene("Voting");
                }
            }
        }
    }

    public async void ConfirmLeave()
    {
        await NetworkController.LeaveLobby(m_roomCode);
        SceneManager.LoadScene("Lobby");

        //NetworkController.LeaveLobby(m_roomCode, success => {
        //    if (success) SceneManager.LoadScene("Lobby");
        //});
    }

    public async void ConfirmReady()
    {
        if (ReadyButton.activeSelf)
        {
            ReadyButton.SetActive(false);
            bool success = await NetworkController.ReadyUp();
            ReadyButton.SetActive(true);
            if (success)
            {
                ReadyButton.GetComponent<Image>().color = Color.yellow;
                foreach (Transform t in ReadyButton.gameObject.transform)
                {
                    var text = t.GetComponent<Text>();
                    if (text != null) text.text = "Waiting...";
                }
            }
        }
    }

    private async void OnSlotChanged(OnlineDatabaseEntry entry, ValueChangedEventArgs args)
    {
        string[] keys = entry.Key.Split('/');
        if (keys.Length >= 5)
        {
            string field = keys[4];

            if (args.Snapshot.Exists && field == "name")
            {
                string value = args.Snapshot.Value.ToString();

                int slot;
                if (!string.IsNullOrEmpty(value) && int.TryParse(keys[3].Replace("slot-", ""), out slot))
                {
                    int player = await NetworkController.GetPlayerNumber(m_roomCode, keys[1]);
                    if (DatabaseButton != null && !StaticClues.SeenSlots.Any(s => s.Equals(new SlotData(player.ToString(), slot.ToString(), value))))
                    {
                        foreach (Transform t in DatabaseButton.transform)
                        {
                            if (t.gameObject.name == "Alert")
                            {
                                t.gameObject.SetActive(true);
                            }
                        }
                    }
                }
            }
        }
    }
}
