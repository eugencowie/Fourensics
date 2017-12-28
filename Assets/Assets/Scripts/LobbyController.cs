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

    private void Start()
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

        Network.GetPlayerLobby(room => {
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
        });
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
    public void SubmitButtonPressed()
    {
        if (!string.IsNullOrEmpty(CodeField.text))
        {
            SwitchPanel(WaitPanel);

            Network.JoinLobby(CodeField.text.ToUpper(), MaxPlayers, success => {
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
            });
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
    public void CreateButtonPressed()
    {
        SwitchPanel(WaitPanel);

        Network.CreateLobbyCode(code => {
            if (string.IsNullOrEmpty(code)) SwitchPanel(StartPanel);
            else {
                Network.CreateLobby(code, createSuccess => {
                    if (!createSuccess) SwitchPanel(StartPanel);
                    else {
                        Network.JoinLobby(code, MaxPlayers, joinSuccess => {
                            if (!joinSuccess) SwitchPanel(StartPanel);
                            else {
                                CodeLabel.text = code;
                                RegisterOnPlayersChanged(code);
                                RegisterOnLobbyStateChanged(code);
                                StartButton.SetActive(true);
                                SwitchPanel(LobbyPanel);
                            }
                        });
                    }
                });
            }
        });
    }

    /// <summary>
    /// Called when the start button in the lobby panel is pressed.
    /// </summary>
    public void StartButtonPressed()
    {
        SwitchPanel(WaitPanel);

        Network.CanStartGame(CodeLabel.text, MaxPlayers, error => {
            if (error != LobbyError.None) {
                if (error == LobbyError.TooFewPlayers) StatusLabel.text = "too few players, requires " + MaxPlayers;
                else if (error == LobbyError.TooManyPlayers) StatusLabel.text = "too many players, requires " + MaxPlayers;
                else StatusLabel.text = "unknown error";
                SwitchPanel(LobbyPanel);
            }
            else Network.AssignPlayerScenes(CodeLabel.text, _ => {
                StaticInventory.Hints.Clear();
                Network.SetLobbyState(CodeLabel.text, LobbyState.InGame);
            }); 
        });
    }

    /// <summary>
    /// Called when the leave button in the lobby panel is pressed.
    /// </summary>
    public void LeaveButtonPressed()
    {
        SwitchPanel(WaitPanel);

        Network.LeaveLobby(CodeLabel.text, success => {
            if (success) {
                DeregisterOnLobbyStateChanged(CodeLabel.text);
                DeregisterOnPlayersChanged(CodeLabel.text);
                CodeLabel.text = "_____";
                SwitchPanel(StartPanel);
            }
            else SwitchPanel(LobbyPanel);
        });
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

    private void OnLobbyStateChanged(object sender, ValueChangedEventArgs args)
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
                    Network.GetPlayerScene(scene => {
                        if (scene >= 1 && scene <= 4) {
                            DeregisterOnLobbyStateChanged(CodeLabel.text);
                            DeregisterOnPlayersChanged(CodeLabel.text);
                            SceneManager.LoadScene(scene);
                        }
                    });
                }
            }
        }
    }
}
