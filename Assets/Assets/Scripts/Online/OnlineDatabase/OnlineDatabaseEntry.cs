using Firebase.Database;
using System;
using System.Threading.Tasks;

/// <summary>
/// Represents a key-value pair in the database.
/// </summary>
public class OnlineDatabaseEntry
{
    public delegate void Listener(OnlineDatabaseEntry entry, ValueChangedEventArgs args);

    private EventHandler<ValueChangedEventArgs> m_listener;

    /// <summary>
    /// The full path to the key in the database.
    /// </summary>
    public readonly string Key;

    /// <summary>
    /// The value of the entry.
    /// </summary>
    public string Value;

    /// <summary>
    /// Initialises the database entry.
    /// </summary>
    public OnlineDatabaseEntry(string key)
    {
        m_listener = null;
        Key = key;
        Value = "";
    }

    /// <summary>
    /// Checks if the key exists in the database.
    /// </summary>
    public async Task<bool> Exists()
    {
        return await OnlineDatabase.Exists(Key);
    }

    /// <summary>
    /// Pulls the value from the database.
    /// </summary>
    public async Task<bool> Pull()
    {
        string result = await OnlineDatabase.Pull(Key);
        if (result != null)
        {
            Value = result;
            return true;
        }
        else return false;
    }

    /// <summary>
    /// Pushes the value to the database.
    /// </summary>
    public async Task<bool> Push()
    {
        return await OnlineDatabase.Push(Key, Value);
    }

    /// <summary>
    /// Deletes the key from the database.
    /// </summary>
    public async Task<bool> Delete()
    {
        bool success = await OnlineDatabase.Delete(Key);
        if (success) Value = "";
        return success;
    }

    /// <summary>
    /// Registers a handler for value changed events.
    /// </summary>
    public void RegisterListener(Listener listener)
    {
        if (m_listener != null)
            DeregisterListener();

        m_listener = (_, args) => { listener(this, args); };
        OnlineDatabase.RegisterListener(Key, m_listener);
    }

    /// <summary>
    /// Deregisters a handler for value changed events.
    /// </summary>
    public void DeregisterListener()
    {
        OnlineDatabase.DeregisterListener(Key, m_listener);
        m_listener = null;
    }
}
