using Firebase.Database;
using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Provides direct low-level access to the Firebase database.
/// </summary>
public class OnlineDatabase
{
    /// <summary>
    /// Reference to the Firebase database root node.
    /// </summary>
    private DatabaseReference m_root;

    /// <summary>
    /// Initialises the database.
    /// </summary>
    public OnlineDatabase()
    {
        m_root = Static.FirebaseDatabase.RootReference;
    }

    /// <summary>
    /// Checks if data exists in the database.
    /// </summary>
    public async Task<bool> Exists(string path)
    {
        DataSnapshot data;
        try { data = await m_root.Child(path).GetValueAsync(); }
        catch { return false; }
        return data.Exists;
    }

    /// <summary>
    /// Pulls data from the database.
    /// </summary>
    public async Task<string> Pull(string path)
    {
        DataSnapshot data;
        try { data = await m_root.Child(path).GetValueAsync(); }
        catch { return null; }
        return data.Value?.ToString();
    }

    /// <summary>
    /// Pushes data to the database.
    /// </summary>
    public async Task<bool> Push(string path, string data)
    {
        try { await m_root.Child(path).SetValueAsync(data); }
        catch { return false; }
        return true;
    }

    /// <summary>
    /// Deletes data from the database.
    /// </summary>
    public async Task<bool> Delete(string path)
    {
        return await Push(path, null);
    }

    /// <summary>
    /// Registers a handler for value changed events.
    /// </summary>
    public void RegisterListener(string path, EventHandler<ValueChangedEventArgs> listener)
    {
        Static.FirebaseDatabase.GetReference(path).ValueChanged += listener;
    }

    /// <summary>
    /// Deregisters a handler for value changed events.
    /// </summary>
    public void DeregisterListener(string path, EventHandler<ValueChangedEventArgs> listener)
    {
        Static.FirebaseDatabase.GetReference(path).ValueChanged -= listener;
    }

    /// <summary>
    /// Tests to check if the database is working properly.
    /// </summary>
    public static async void RunTests()
    {
        OnlineDatabase db = new OnlineDatabase();

        Debug.Assert(await db.Exists("test/does/not/exist") == false);

        Debug.Assert(await db.Push("test/data/key", "value") == true);
        Debug.Assert(await db.Exists("test/data") == true);
        Debug.Assert(await db.Exists("test/data/key") == true);
        Debug.Assert(await db.Pull("test/data/key") == "value");

        Debug.Assert(await db.Delete("test") == true);
        Debug.Assert(await db.Exists("test") == false);
    }
}
