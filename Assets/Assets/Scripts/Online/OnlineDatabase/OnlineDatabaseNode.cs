using Firebase.Database;
using System;

/// <summary>
/// Represents a node in the database which contains a collection of database entries.
/// </summary>
public abstract class OnlineDatabaseNode
{
    /// <summary>
    /// Reference to the database.
    /// </summary>
    protected readonly OnlineDatabase Database;

    /// <summary>
    /// The full path to the node in the database.
    /// </summary>
    public readonly string Key;

    /// <summary>
    /// Initialises the database node.
    /// </summary>
    public OnlineDatabaseNode(OnlineDatabase database, string key)
    {
        Database = database;
        Key = key;
    }

    /// <summary>
    /// An enumerable collection of database entries.
    /// </summary>
    public abstract OnlineDatabaseEntry[] Entries { get; }

    /// <summary>
    /// Checks if the key exists in the database. This is an asynchronous operation which will call
    /// the specified action on completion.
    /// </summary>
    public void Exists(Action<bool> returnExists)
    {
        Database.Exists(Key, returnExists);
    }

    /// <summary>
    /// Deletes the key from the database. This is an asynchronous operation which will call
    /// the specified action on completion.
    /// </summary>
    public void Delete(Action<bool> returnSuccess=null)
    {
        Database.Delete(Key, success => {
            if (success) {
                foreach (var entry in Entries)
                    entry.Value = "";
            }
            returnSuccess(success);
        });
    }

    /// <summary>
    /// Pulls all entries from the database. This is an asynchronous operation which will call
    /// the specified action on completion.
    /// </summary>
    public void PullEntries(Action<bool> returnSuccess=null)
    {
        OnlineDatabase.ValidateAction(ref returnSuccess);

        Action<bool> returnHandler = ReturnHandler(Entries.Length, returnSuccess);

        foreach (var entry in Entries)
        {
            entry.Pull(returnHandler);
        }
    }

    /// <summary>
    /// Pushes all entries to the database. This is an asynchronous operation which will call
    /// the specified action on completion.
    /// </summary>
    public void PushEntries(Action<bool> returnSuccess=null)
    {
        OnlineDatabase.ValidateAction(ref returnSuccess);

        Action<bool> returnHandler = ReturnHandler(Entries.Length, returnSuccess);

        foreach (var entry in Entries)
        {
            entry.Push(returnHandler);
        }
    }

    /// <summary>
    /// Deletes all entries from the database. This is an asynchronous operation which will call
    /// the specified action on completion.
    /// </summary>
    public void DeleteEntries(Action<bool> returnSuccess=null)
    {
        OnlineDatabase.ValidateAction(ref returnSuccess);

        Action<bool> returnHandler = ReturnHandler(Entries.Length, returnSuccess);

        foreach (var entry in Entries)
        {
            entry.Delete(returnHandler);
        }
    }

    public void RegisterListeners(OnlineDatabaseEntry.Listener listener)
    {
        foreach (var entry in Entries)
        {
            entry.RegisterListener(listener);
        }
    }

    public void DeregisterListeners()
    {
        foreach (var entry in Entries)
        {
            entry.DeregisterListener();
        }
    }

    private Action<bool> ReturnHandler(int count, Action<bool> returnSuccess)
    {
        int progress = 0;
        bool total = true;
        return success => {
            progress++;
            if (!success) total = false;
            if (progress >= count-1) returnSuccess(total);
        };
    }
}
