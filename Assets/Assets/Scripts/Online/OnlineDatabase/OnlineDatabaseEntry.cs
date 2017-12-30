using Firebase.Database;
using System;
using System.Threading.Tasks;

/// <summary>
/// Represents a key-value pair in the database.
/// </summary>
public class OnlineDatabaseEntry
{
    public delegate void Listener(OnlineDatabaseEntry entry, ValueChangedEventArgs args);

    /// <summary>
    /// Reference to the database.
    /// </summary>
    private readonly OnlineDatabase m_database;

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
    public OnlineDatabaseEntry(OnlineDatabase database, string key)
    {
        m_database = database;
        m_listener = null;
        Key = key;
        Value = "";
    }

    /// <summary>
    /// Checks if the key exists in the database.
    /// </summary>
    public async Task<bool> Exists()
    {
        return await m_database.Exists(Key);
    }

    /// <summary>
    /// Pulls the value from the database.
    /// </summary>
    public async Task<bool> Pull()
    {
        string result = await m_database.Pull(Key);
            if (result != null) {
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
        return await m_database.Push(Key, Value);
    }

    /// <summary>
    /// Deletes the key from the database.
    /// </summary>
    public async Task<bool> Delete()
    {
        bool success = await m_database.Delete(Key);
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
        m_database.RegisterListener(Key, m_listener);
    }

    /// <summary>
    /// Deregisters a handler for value changed events.
    /// </summary>
    public void DeregisterListener()
    {
        m_database.DeregisterListener(Key, m_listener);
        m_listener = null;
    }
}
