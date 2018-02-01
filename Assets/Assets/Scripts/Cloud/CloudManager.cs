using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

static class CloudManager
{
    /// <summary>
    /// If lobby exists, updates player entry to new lobby and adds player to lobby.
    /// </summary>
    public static bool JoinLobby(Lobby lobby, int maxPlayers)
    {
        // Check if lobby exists
        if (lobby.State.Value == null)
            return false;

        // Get list of players in lobby
        List<string> players = lobby.Users
            .Where(u => u.Value != null)
            .Select(u => u.Value)
            .ToList();

        // If player is already in room
        if (players.Contains(SignInScene.User.Id))
            return true;

        // If too many players
        if (players.Count >= maxPlayers)
            return false;

        // Add player to lobby
        players.Add(SignInScene.User.Id);
        for (int i = 0; i < players.Count; i++)
            lobby.Users[i].Value = players[i];

        return true;
    }

    private static async Task<bool> Exists(string path)
    {
        DataSnapshot data;
        try { data = await Cloud.Database.RootReference.Child(path).GetValueAsync(); }
        catch { return false; }
        return data.Exists;
    }

    /// <summary>
    /// Attempts to generate a lobby code which is not in use. If all codes generated are in
    /// use, returns null.
    /// </summary>
    public static async Task<string> CreateLobbyCode()
    {
        for (int i = 0; i < 3; i++)
        {
            string code = GenerateRandomCode();
            string key = $"lobbies/{code}/state";

            if (!await Exists(key))
            {
                return code;
            }
        }

        return null;
    }

    /// <summary>
    /// Deletes the player entry and removes the player from the lobby. If no players are left in the
    /// lobby, deletes the lobby.
    /// </summary>
    public static void LeaveLobby()
    {
        SignInScene.User.Lobby.Value = null;
        SignInScene.User.Scene.Value = 0;
        SignInScene.User.Ready.Value = false;
        SignInScene.User.Vote.Value = null;
        foreach (var item in SignInScene.User.Items)
        {
            item.Name.Value = null;
            item.Description.Value = null;
            item.Image.Value = null;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public static async Task<int> AssignPlayerScenes(string code)
    {
        List<string> players = LobbyScene.Lobby.Users.Select(u => u.Value).ToList();
        players.RemoveAll(s => string.IsNullOrEmpty(s));
        players = players.OrderBy(_ => UnityEngine.Random.value).ToList();
        int ourScene = -1;
        for (int i = 0; i < players.Count; i++)
        {
            User player = await User.Fetch(players[i]);
            if (player.Id == SignInScene.User.Id)
            {
                ourScene = (i + 1);
            }
            player.Scene.Value = i + 1;
        }
        return ourScene;
    }
    
    public static void UploadDatabaseItem(int slot, ObjectHintData hint)
    {
        SignInScene.User.Items[slot - 1].Name.Value = hint.Name;
        SignInScene.User.Items[slot - 1].Description.Value = hint.Hint;
        SignInScene.User.Items[slot - 1].Image.Value = hint.Image;
    }

    public static void RemoveDatabaseItem(int slot)
    {
        UploadDatabaseItem(slot, new ObjectHintData("", "", ""));
    }

    public static int GetPlayerNumber(string player)
    {
        List<string> players = LobbyScene.Lobby.Users.Select(u => u.Value).ToList();
        players.RemoveAll(s => string.IsNullOrEmpty(s));
        players.Remove(SignInScene.User.Id);
        players.Insert(0, SignInScene.User.Id);
        int playerNb = players.IndexOf(player);
        if (playerNb >= 0 && playerNb < players.Count)
        {
            return playerNb;
        }
        else return -1;
    }

    public static async Task<User> DownloadClues(int playerNb)
    {
        List<string> players = LobbyScene.Lobby.Users.Select(u => u.Value).ToList();
        players.RemoveAll(s => string.IsNullOrEmpty(s));
        players.Remove(SignInScene.User.Id);
        players.Insert(0, SignInScene.User.Id);
        if (playerNb < players.Count)
        {
            return await User.Fetch(players[playerNb]);
        }
        else return null;
    }
    
    public static IEnumerable<string> AllUsers => LobbyScene.Lobby.Users
            .Where(user => !string.IsNullOrWhiteSpace(user.Value))
            .Select(user => user.Value);

    public static IEnumerable<string> OtherUsers => LobbyScene.Lobby.Users
            .Where(user => !string.IsNullOrWhiteSpace(user.Value))
            .Where(user => user.Value != SignInScene.User.Id)
            .Select(user => user.Value);
    
    public static async Task<User[]> FetchUsers(IEnumerable<string> userIds)
    {
        return await Task.WhenAll(userIds.Select(id => User.Fetch(id)));
    }
    
    /// <summary>
    /// Generates a random five-character room code.
    /// </summary>
    private static string GenerateRandomCode()
    {
        const string validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        string roomCode = "";

        for (int i = 0; i < 5; i++)
        {
            // Gets a random valid character and adds it to the room code string
            int randomIndex = UnityEngine.Random.Range(0, validChars.Length - 1);
            char randomChar = validChars[randomIndex];
            roomCode += randomChar;
        }

        return roomCode;
    }
}
