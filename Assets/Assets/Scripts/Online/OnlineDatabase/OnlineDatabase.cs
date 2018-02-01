using Firebase.Database;
using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Provides direct low-level access to the Firebase database.
/// </summary>
static class OnlineDatabase
{
    /// <summary>
    /// Reference to the Firebase database root node.
    /// </summary>
    private static DatabaseReference m_root => Cloud.Database.RootReference;

    /// <summary>
    /// Checks if data exists in the database.
    /// </summary>
    public static async Task<bool> Exists(string path)
    {
        DataSnapshot data;
        try { data = await m_root.Child(path).GetValueAsync(); }
        catch { return false; }
        return data.Exists;
    }

    /// <summary>
    /// Pulls data from the database.
    /// </summary>
    public static async Task<string> Pull(string path)
    {
        DataSnapshot data;
        try { data = await m_root.Child(path).GetValueAsync(); }
        catch { return null; }
        return data.Value?.ToString();
    }

    /// <summary>
    /// Pushes data to the database.
    /// </summary>
    public static async Task<bool> Push(string path, string data)
    {
        try { await m_root.Child(path).SetValueAsync(data); }
        catch { return false; }
        return true;
    }

    /// <summary>
    /// Deletes data from the database.
    /// </summary>
    public static async Task<bool> Delete(string path)
    {
        return await Push(path, null);
    }

    /// <summary>
    /// Registers a handler for value changed events.
    /// </summary>
    public static void RegisterListener(string path, EventHandler<ValueChangedEventArgs> listener)
    {
        Cloud.Database.GetReference(path).ValueChanged += listener;
    }

    /// <summary>
    /// Deregisters a handler for value changed events.
    /// </summary>
    public static void DeregisterListener(string path, EventHandler<ValueChangedEventArgs> listener)
    {
        Cloud.Database.GetReference(path).ValueChanged -= listener;
    }

    /// <summary>
    /// Tests to check if the database is working properly.
    /// </summary>
    public static async void RunTests()
    {
        Debug.Assert(await Exists("test/does/not/exist") == false);

        Debug.Assert(await Push("test/data/key", "value") == true);
        Debug.Assert(await Exists("test/data") == true);
        Debug.Assert(await Exists("test/data/key") == true);
        Debug.Assert(await Pull("test/data/key") == "value");

        Debug.Assert(await Delete("test") == true);
        Debug.Assert(await Exists("test") == false);
    }
}
