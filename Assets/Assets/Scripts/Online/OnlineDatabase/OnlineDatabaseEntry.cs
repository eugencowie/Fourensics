using Firebase.Database;
using System;

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
    /// Checks if the key exists in the database. This is an asynchronous operation which will call
    /// the specified action on completion.
    /// </summary>
    public void Exists(Action<bool> returnExists)
    {
        m_database.Exists(Key, returnExists);
    }

    /// <summary>
    /// Pulls the value from the database. This is an asynchronous operation which will call the
    /// specified action on completion.
    /// </summary>
    public void Pull(Action<bool> returnSuccess=null)
    {
        OnlineDatabase.ValidateAction(ref returnSuccess);

        m_database.Pull(Key, result => {
            if (result != null) {
                Value = result;
                returnSuccess(true);
            }
            else returnSuccess(false);
        });
    }

    /// <summary>
    /// Pushes the value to the database. This is an asynchronous operation which will call the
    /// specified action on completion.
    /// </summary>
    public void Push(Action<bool> returnSuccess=null)
    {
        m_database.Push(Key, Value, returnSuccess);
    }

    /// <summary>
    /// Deletes the key from the database. This is an asynchronous operation which will call
    /// the specified action on completion.
    /// </summary>
    public void Delete(Action<bool> returnSuccess=null)
    {
        m_database.Delete(Key, success => {
            if (success) Value = "";
            returnSuccess(success);
        });
    }

    public void RegisterListener(Listener listener)
    {
        m_listener = (_, args) => { listener(this, args); };
        m_database.RegisterListener(Key, m_listener);
    }

    public void DeregisterListener()
    {
        m_database.DeregisterListener(Key, m_listener);
    }
}
