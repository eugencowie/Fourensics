using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public enum LobbyState { Lobby, InGame, Voting, Finished }

public enum LobbyError { None, Unknown, TooFewPlayers, TooManyPlayers }

public class OnlineManager
{
    private OnlineDatabase m_database;
    private Player m_player;
    private Lobby m_lobby;

    public OnlineManager()
    {
        m_database = new OnlineDatabase();
        m_player = new Player(m_database, SignIn.GetPlayerId());
        m_lobby = null;
    }

    #region Async methods

    /// <summary>
    /// If the player is listed as being in a lobby and that lobby does exist, returns the lobby
    /// code. Otherwise, returns null.
    /// </summary>
    public async Task<string> GetPlayerLobby()
    {
        // If 'players/{0}/lobby' exists and 'lobbies/{1}' exists, return lobby code.
        bool success = await m_player.Lobby.Pull();
            if (success) {
                m_lobby = new Lobby(m_database, m_player.Lobby.Value);
                bool exists = await m_lobby.Exists();
                    if (exists) return m_lobby.Id;
                    else { await m_player.Delete(); return null; }
            }
            else return null;
    }

    /// <summary>
    /// If the player is listed as being in a scene and that scene does exist, returns the scene
    /// number. Otherwise, returns 0.
    /// </summary>
    public async Task<int> GetPlayerScene()
    {
        // If 'players/{0}/scene' exists, return it.
        bool success = await m_player.Scene.Pull();
            if (success) {
                int scene;
                if (int.TryParse(m_player.Scene.Value, out scene)) return scene;
                else return 0;
            }
            else return 0;
    }

    /// <summary>
    /// If lobby exists, updates player entry to new lobby and adds player to lobby.
    /// </summary>
    public async Task<bool> JoinLobby(string code, int maxPlayers)
    {
        // If 'lobbies/{0}' exists, push 'players/{1}/lobby', get 'lobbies/{0}/players', add
        // player to list and push 'lobbies/{0}/players' back up.
        m_lobby = new Lobby(m_database, code);
        bool exists = await m_lobby.Exists();
            if (exists) {
                bool lobbySuccess = await m_lobby.Players.Pull();
                    if (lobbySuccess) {
                        List<string> players = m_lobby.Players.Value.Split(',').ToList();
                        if (players.Count < maxPlayers && !players.Contains(m_player.Id)) {
                            m_player.Lobby.Value = code;
                            bool playerSuccess = await m_player.Lobby.Push();
                                if (playerSuccess) {
                                    players.Add(m_player.Id);
                                    players.RemoveAll(s => string.IsNullOrEmpty(s));
                                    m_lobby.Players.Value = string.Join(",", players.ToArray());
                                    bool roomPlayersSuccess = await m_lobby.Players.Push();
                                        if (roomPlayersSuccess) {
                                            return await m_player.Clues.PushEntries();
                                        }
                                        else return false;
                                }
                                else return false;
                        }
                        else return false;
                    }
                    else return false;
            }
            else return false;
    }

    /// <summary>
    /// Creates a lobby on the server.
    /// </summary>
    public async Task<bool> CreateLobby(string code)
    {
        m_lobby = new Lobby(m_database, code);

        m_lobby.CreatedTime.Value = DateTimeOffset.UtcNow.ToString("o");
        m_lobby.State.Value = ((int)LobbyState.Lobby).ToString();

        bool success1 = await m_lobby.CreatedTime.Push();
            bool success2 = await m_lobby.Players.Push();
                bool success3 = await m_lobby.State.Push();
                    return (success1 && success2 && success3);
    }

    /// <summary>
    /// Attempts to generate a lobby code which is not in use. If all codes generated are in
    /// use, returns null.
    /// </summary>
    public async Task<string> CreateLobbyCode()
    {
        // Three attempts to find a unique room code.
        string[] codes = { GenerateRandomCode(), GenerateRandomCode(), GenerateRandomCode() };
        List<string> keys = codes.Select(c => new Lobby(m_database, c).Key).ToList();

        // If the generated room code already exists, try again (up to three tries).
        bool exists0 = await m_database.Exists(keys[0]);
            if (!exists0) return codes[0];
            else { bool exists1 = await m_database.Exists(keys[1]);
                if (!exists1) return codes[1];
                else { bool exists2 = await m_database.Exists(keys[2]);
                    if (!exists2) return codes[2];
                    else return null;
                }
            }
    }

    /// <summary>
    /// Deletes the player entry and removes the player from the lobby. If no players are left in the
    /// lobby, deletes the lobby.
    /// </summary>
    public async Task<bool> LeaveLobby(string code)
    {
        // Delete 'players/{0}', pull 'lobbies/{1}/players', remove the player from list and push
        // 'lobbies/{1}/players' back up (unless there are no players left, then delete the lobby).
        /*m_player.Delete(success1 => {
            if (success1) {
                //m_lobby = new Lobby(m_database, code); // TODO
                m_lobby.Players.Pull(success2 => {
                    if (success2) {
                        //List<string> layers = m_lobby.Players.Value.Split(',').ToList();
                        //layers.Remove(m_player.Id);
                        //layers.RemoveAll(s => string.IsNullOrEmpty(s));
                        //if (layers.Count > 0) {
                        //    m_lobby.Players.Value = string.Join(",", layers.ToArray());
                        //    m_lobby.Players.Push(returnSuccess);
                        //} else {
                            m_lobby.Delete(returnSuccess);
                        //}
                    }
                    else returnSuccess(false);
                });
            }
            else returnSuccess(false);
        });*/

        return await m_player.Delete();
    }

    /// <summary>
    /// Checks if the lobby has the required number of players.
    /// </summary>
    public async Task<LobbyError> CanStartGame(string code, int requiredPlayers)
    {
        // TODO: remove this in final build
        return LobbyError.None;

        //m_lobby = new Lobby(m_database, code); // TODO
        bool success = await m_lobby.Players.Pull();
            if (success) {
                List<string> players = m_lobby.Players.Value.Split(',').ToList();
                players.RemoveAll(s => string.IsNullOrEmpty(s));
                if (players.Count < requiredPlayers) return LobbyError.TooFewPlayers;
                else if (players.Count > requiredPlayers) return LobbyError.TooManyPlayers;
                else return LobbyError.None;
            }
            else return LobbyError.Unknown;
    }

    /// <summary>
    /// Pushes a new lobby state to the server.
    /// </summary>
    public async Task<bool> SetLobbyState(string code, LobbyState state)
    {
        //m_lobby = new Lobby(m_database, code); // TODO
        m_lobby.State.Value = ((int)state).ToString();
        return await m_lobby.State.Push();
    }

    /// <summary>
    ///
    /// </summary>
    public async Task<int> AssignPlayerScenes(string code)
    {
        //m_lobby = new Lobby(m_database, code); // TODO
        bool success = await m_lobby.Players.Pull();
            if (success) {
                List<string> players = m_lobby.Players.Value.Split(',').ToList();
                players.RemoveAll(s => string.IsNullOrEmpty(s));
                players = players.OrderBy(_ => UnityEngine.Random.value).ToList();
                int ourScene = -1;
                for (int i = 0; i < players.Count; i++) {
                    Player player = new Player(m_database, players[i]);
                    if (player.Id == m_player.Id) {
                        ourScene = (i+1);
                    }
                    player.Scene.Value = (i+1).ToString();
                    await player.Scene.Push();
                }
                return ourScene;
            }
            else return -1;
    }

    #endregion

    #region Async database methods

    public async Task<bool> UploadDatabaseItem(int slot, ObjectHintData hint)
    {
        m_player.Clues.Clues[slot-1].Name.Value = hint.Name;
        m_player.Clues.Clues[slot-1].Hint.Value = hint.Hint;
        m_player.Clues.Clues[slot-1].Image.Value = hint.Image;
        return await m_player.Clues.Clues[slot-1].PushEntries();
    }

    public async Task<bool> RemoveDatabaseItem(int slot)
    {
        return await m_player.Clues.Clues[slot - 1].Delete();
    }

    public async void RegisterCluesChanged(string code, OnlineDatabaseEntry.Listener listener)
    {
        Lobby lobby = new Lobby(m_database, code); // TODO
        bool success = await lobby.Players.Pull();
            if (success) {
                string val = lobby.Players.Value;
                List<string> players = lobby.Players.Value.Split(',').ToList();
                players.RemoveAll(s => string.IsNullOrEmpty(s));
                players.Remove(m_player.Id);
                foreach (string playerId in players) {
                    Player player = new Player(m_database, playerId);
                    foreach (PlayerClue clue in player.Clues.Clues) {
                        clue.RegisterListeners(listener);
                    }
                }
            }
    }

    public async void DeregisterCluesChanged() // TODO: make all these functions static
    {
        bool success = await m_lobby.Players.Pull();
            if (success) {
                List<string> players = m_lobby.Players.Value.Split(',').ToList();
                players.RemoveAll(s => string.IsNullOrEmpty(s));
                players.Remove(m_player.Id);
                foreach (string playerId in players) {
                    Player player = new Player(m_database, playerId);
                    foreach (PlayerClue clue in player.Clues.Clues) {
                        clue.DeregisterListeners();
                    }
                }
            }
    }

    public async Task<int> GetPlayerNumber(string code, string player)
    {
        //m_lobby = new Lobby(m_database, code); // TODO
        bool success = await m_lobby.Players.Pull();
            if (success) {
                List<string> players = m_lobby.Players.Value.Split(',').ToList();
                players.RemoveAll(s => string.IsNullOrEmpty(s));
                players.Remove(m_player.Id);
                players.Insert(0, m_player.Id);
                int playerNb = players.IndexOf(player);
                if (playerNb >= 0 && playerNb < players.Count) {
                    return playerNb;
                }
                else return -1;
            }
            else return -1;
    }

    public async Task<Player> DownloadClues(string code, int playerNb)
    {
        //m_lobby = new Lobby(m_database, code); // TODO
        bool success = await m_lobby.Players.Pull();
            if (success) {
                List<string> players = m_lobby.Players.Value.Split(',').ToList();
                players.RemoveAll(s => string.IsNullOrEmpty(s));
                players.Remove(m_player.Id);
                players.Insert(0, m_player.Id);
                if (playerNb < players.Count) {
                    Player player = new Player(m_database, players[playerNb]);
                    bool pullSuccess = await player.Clues.PullEntries();
                        if (pullSuccess) return player;
                        else return null;
                }
                else return null;
            }
            else return null;
    }

    #endregion

    #region Async voting methods

    public async Task<string[]> GetPlayers(string code)
    {
        m_lobby = new Lobby(m_database, code); // TODO
        bool success = await m_lobby.Players.Pull();
            if (success)
            {
                List<string> players = m_lobby.Players.Value.Split(',').ToList();
                players.RemoveAll(s => string.IsNullOrEmpty(s));
                return players.ToArray();
            }
            else return null;
    }

    public async Task<bool> ReadyUp()
    {
        m_player.Ready.Value = "true";
        return await m_player.Ready.Push();
    }

    public async Task<bool> SubmitVote(string suspect)
    {
        m_player.Vote.Value = suspect;
        return await m_player.Vote.Push();
    }

    public async void RegisterReadyChanged(string code, OnlineDatabaseEntry.Listener listener)
    {
        m_lobby = new Lobby(m_database, code); // TODO
        bool success = await m_lobby.Players.Pull();
            if (success) {
                List<string> players = m_lobby.Players.Value.Split(',').ToList();
                players.RemoveAll(s => string.IsNullOrEmpty(s));
                //players.Remove(m_player.Id);
                foreach (string playerId in players) {
                    Player player = new Player(m_database, playerId);
                    player.Ready.RegisterListener(listener);
                }
            }
    }

    public async void DeregisterReadyChanged(string code) // TODO: make all these functions static
    {
        m_lobby = new Lobby(m_database, code); // TODO
        bool success = await m_lobby.Players.Pull();
            if (success) {
                List<string> players = m_lobby.Players.Value.Split(',').ToList();
                players.RemoveAll(s => string.IsNullOrEmpty(s));
                //players.Remove(m_player.Id);
                foreach (string playerId in players) {
                    Player player = new Player(m_database, playerId);
                    player.Ready.DeregisterListener();
                }
            }
    }

    public async void RegisterVoteChanged(string code, OnlineDatabaseEntry.Listener listener)
    {
        m_lobby = new Lobby(m_database, code); // TODO
        bool success = await m_lobby.Players.Pull();
            if (success) {
                List<string> players = m_lobby.Players.Value.Split(',').ToList();
                players.RemoveAll(s => string.IsNullOrEmpty(s));
                //players.Remove(m_player.Id);
                foreach (string playerId in players) {
                    Player player = new Player(m_database, playerId);
                    player.Vote.RegisterListener(listener);
                }
            }
    }

    public async void DeregisterVoteChanged(string code) // TODO: make all these functions static
    {
        m_lobby = new Lobby(m_database, code); // TODO
        bool success = await m_lobby.Players.Pull();
            if (success) {
                List<string> players = m_lobby.Players.Value.Split(',').ToList();
                players.RemoveAll(s => string.IsNullOrEmpty(s));
                //players.Remove(m_player.Id);
                foreach (string playerId in players) {
                    Player player = new Player(m_database, playerId);
                    player.Vote.DeregisterListener();
                }
            }
    }

    #endregion

    #region Listeners

    public void RegisterListener(string path, EventHandler<ValueChangedEventArgs> listener)
    {
        m_database.RegisterListener(path, listener);
    }

    public void DeregisterListener(string path, EventHandler<ValueChangedEventArgs> listener)
    {
        m_database.DeregisterListener(path, listener);
    }

    #endregion

    #region Utility methods
    
    /// <summary>
    /// Generates a random five-character room code.
    /// </summary>
    private static string GenerateRandomCode()
    {
        const string validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        string roomCode = "";

        for (int i = 0; i < 5; i++)
        {
            // Gets a random valid character and adds it to the room code string.
            int randomIndex = UnityEngine.Random.Range(0, validChars.Length - 1);
            char randomChar = validChars[randomIndex];
            roomCode += randomChar;
        }

        return roomCode;
    }

    #endregion
}
