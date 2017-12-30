using Firebase.Database;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyController : MonoBehaviour
{
    [Tooltip("GameObject with a Text component to display the generated room code")]
    public GameObject CodeLabelObject;
    private Text CodeLabel
    {
        get { return CodeLabelObject.GetComponent<Text>(); }
    }

    [Tooltip("GameObject with an InputField component to input the room code to join")]
    public GameObject CodeFieldObject;
    private InputField CodeField
    {
        get { return CodeFieldObject.GetComponent<InputField>(); }
    }

    [Tooltip("GameObject with a Text component to display the list of players in the room")]
    public GameObject PlayersLabelObject;
    private Text PlayersLabel
    {
        get { return PlayersLabelObject.GetComponent<Text>(); }
    }

    [Tooltip("GameObject with a Text component to display the status of the room")]
    public GameObject StatusLabelObject;
    private Text StatusLabel
    {
        get { return StatusLabelObject.GetComponent<Text>(); }
    }

    public GameObject StartPanel;
    public GameObject JoinPanel;
    public GameObject LobbyPanel;
    public GameObject WaitPanel;
    public GameObject StartButton;

    [Range(1,4)]
    public int MaxPlayers = 4;

    private OnlineManager Network;

    async void Start()
    {
        //StaticInventory.Hints.Clear();
        //StaticSuspects.DiscardedSuspects.Clear();
        //StaticClues.SeenSlots.Clear();

        Network = new OnlineManager();

        /*SwitchPanel(WaitPanel);

        Network.GetPlayerLobby(room => {
            if (string.IsNullOrEmpty(room)) SwitchPanel(StartPanel);
            else {
                Network.LeaveLobby(room, _ => {
                    SwitchPanel(StartPanel);
                });
            }
        });*/

        SwitchPanel(WaitPanel);

        string room = await Network.GetPlayerLobby();
            if (string.IsNullOrEmpty(room)) {
                StaticClues.Reset();
                StaticInventory.Reset();
                StaticRoom.Reset();
                StaticSlot.Reset();
                StaticSuspects.Reset();
                SwitchPanel(StartPanel);
            } else {
                CodeLabel.text = room;
                RegisterOnPlayersChanged(room);
                RegisterOnLobbyStateChanged(room);
                SwitchPanel(LobbyPanel);
            }
    }

    /// <summary>
    /// Called when the join button in the start panel is pressed.
    /// </summary>
    public void JoinButtonPressed()
    {
        SwitchPanel(JoinPanel);
    }

    /// <summary>
    /// Called when the submit button in the join panel is pressed.
    /// </summary>
    public async void SubmitButtonPressed()
    {
        if (!string.IsNullOrEmpty(CodeField.text))
        {
            SwitchPanel(WaitPanel);

            bool success = await Network.JoinLobby(CodeField.text.ToUpper(), MaxPlayers);
                if (!success) {
                    CodeField.text = "";
                    SwitchPanel(JoinPanel);
                } else {
                    CodeLabel.text = CodeField.text.ToUpper();
                    RegisterOnPlayersChanged(CodeLabel.text);
                    RegisterOnLobbyStateChanged(CodeLabel.text);
                    StartButton.SetActive(false);
                    SwitchPanel(LobbyPanel);
                }
        }
    }

    /// <summary>
    /// Called when the join button in the start panel is pressed.
    /// </summary>
    public void BackButtonPressed()
    {
        SwitchPanel(StartPanel);
    }

    /// <summary>
    /// Called when the create button in the start panel is pressed.
    /// </summary>
    public async void CreateButtonPressed()
    {
        SwitchPanel(WaitPanel);

        string code = await Network.CreateLobbyCode();
            if (string.IsNullOrEmpty(code)) SwitchPanel(StartPanel);
            else {
                bool createSuccess = await Network.CreateLobby(code);
                    if (!createSuccess) SwitchPanel(StartPanel);
                    else {
                        bool joinSuccess = await Network.JoinLobby(code, MaxPlayers);
                            if (!joinSuccess) SwitchPanel(StartPanel);
                            else {
                                CodeLabel.text = code;
                                RegisterOnPlayersChanged(code);
                                RegisterOnLobbyStateChanged(code);
                                StartButton.SetActive(true);
                                SwitchPanel(LobbyPanel);
                            }
                    }
            }
    }

    /// <summary>
    /// Called when the start button in the lobby panel is pressed.
    /// </summary>
    public async void StartButtonPressed()
    {
        SwitchPanel(WaitPanel);

        LobbyError error = await Network.CanStartGame(CodeLabel.text, MaxPlayers);
            if (error != LobbyError.None) {
                if (error == LobbyError.TooFewPlayers) StatusLabel.text = "too few players, requires " + MaxPlayers;
                else if (error == LobbyError.TooManyPlayers) StatusLabel.text = "too many players, requires " + MaxPlayers;
                else StatusLabel.text = "unknown error";
                SwitchPanel(LobbyPanel);
            }
            else { await Network.AssignPlayerScenes(CodeLabel.text);
                StaticInventory.Hints.Clear();
                await Network.SetLobbyState(CodeLabel.text, LobbyState.InGame);
            }
    }

    /// <summary>
    /// Called when the leave button in the lobby panel is pressed.
    /// </summary>
    public async void LeaveButtonPressed()
    {
        SwitchPanel(WaitPanel);

        bool success = await Network.LeaveLobby(CodeLabel.text);
            if (success) {
                DeregisterOnLobbyStateChanged(CodeLabel.text);
                DeregisterOnPlayersChanged(CodeLabel.text);
                CodeLabel.text = "_____";
                SwitchPanel(StartPanel);
            }
            else SwitchPanel(LobbyPanel);
    }

    public void CodeFieldChanged(string s)
    {
        CodeField.text = CodeField.text.ToUpper();
    }

    private void SwitchPanel(GameObject panel)
    {
        // Disable all panels
        foreach (var p in new GameObject[] { StartPanel, JoinPanel, LobbyPanel, WaitPanel })
        {
            p.SetActive(false);
        }

        // Enable specified panel
        panel.SetActive(true);
    }

    private void RegisterOnPlayersChanged(string lobby)
    {
        string roomPlayersKey = string.Format("lobbies/{0}/players", lobby);
        Network.RegisterListener(roomPlayersKey, OnPlayersChanged);
    }

    private void DeregisterOnPlayersChanged(string lobby)
    {
        string roomPlayersKey = string.Format("lobbies/{0}/players", lobby);
        Network.DeregisterListener(roomPlayersKey, OnPlayersChanged);
    }

    private void OnPlayersChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.Snapshot.Exists)
        {
            PlayersLabel.text = args.Snapshot.Value.ToString().Replace(',', '\n');
        }
    }

    private void RegisterOnLobbyStateChanged(string lobby)
    {
        string roomStateKey = string.Format("lobbies/{0}/state", lobby);
        Network.RegisterListener(roomStateKey, OnLobbyStateChanged);
    }

    private void DeregisterOnLobbyStateChanged(string lobby)
    {
        string roomStateKey = string.Format("lobbies/{0}/state", lobby);
        Network.DeregisterListener(roomStateKey, OnLobbyStateChanged);
    }

    private async void OnLobbyStateChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.Snapshot.Exists)
        {
            string statusStr = args.Snapshot.Value.ToString();
            int statusNr = -1;
            if (int.TryParse(statusStr, out statusNr))
            {
                LobbyState state = (LobbyState)statusNr;
                if (state == LobbyState.InGame)
                {
                    int scene = await Network.GetPlayerScene();
                        if (scene >= 1 && scene <= 4) {
                            DeregisterOnLobbyStateChanged(CodeLabel.text);
                            DeregisterOnPlayersChanged(CodeLabel.text);
                            SceneManager.LoadScene(scene);
                        }
                }
            }
        }
    }
}
