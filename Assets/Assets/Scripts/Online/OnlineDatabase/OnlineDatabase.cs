using Firebase;
using Firebase.Database;
using Firebase.Unity.Editor;
using System;
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
        FirebaseApp.DefaultInstance.SetEditorDatabaseUrl("https://fourensics-game.firebaseio.com");
        m_root = FirebaseDatabase.DefaultInstance.RootReference;
    }

    /// <summary>
    /// Checks if data exists in the database. This is an asynchronous operation which will call
    /// the specified action on completion.
    /// </summary>
    public void Exists(string path, Action<bool> returnExists)
    {
        ValidateAction(ref returnExists);

        m_root.Child(path).GetValueAsync().ContinueWith(t => {
            returnExists(t.Result.Exists);
        });
    }

    /// <summary>
    /// Pulls data from the database. This is an asynchronous operation which will call the
    /// specified action on completion.
    /// </summary>
    public void Pull(string path, Action<string> returnResult)
    {
        ValidateAction(ref returnResult);

        m_root.Child(path).GetValueAsync().ContinueWith(t => {
            if (t.Result.Exists) returnResult(t.Result.Value.ToString());
            else returnResult(null);
        });
    }

    /// <summary>
    /// Pushes data to the database. This is an asynchronous operation which will call the
    /// specified action on completion.
    /// </summary>
    public void Push(string path, string data, Action<bool> returnSuccess=null)
    {
        ValidateAction(ref returnSuccess);

        m_root.Child(path).SetValueAsync(data).ContinueWith(t => {
            returnSuccess(t.IsCompleted);
        });
    }

    /// <summary>
    /// Deletes data from the database. This is an asynchronous operation which will call
    /// the specified action on completion.
    /// </summary>
    public void Delete(string path, Action<bool> returnSuccess=null)
    {
        Push(path, null, returnSuccess);
    }

    /// <summary>
    /// Register a handler for value changed events.
    /// </summary>
    public void RegisterListener(string path, EventHandler<ValueChangedEventArgs> listener)
    {
        FirebaseDatabase.DefaultInstance.GetReference(path).ValueChanged += listener;
    }

    /// <summary>
    /// Deregister a handler for value changed events.
    /// </summary>
    public void DeregisterListener(string path, EventHandler<ValueChangedEventArgs> listener)
    {
        FirebaseDatabase.DefaultInstance.GetReference(path).ValueChanged -= listener;
    }

    /// <summary>
    /// If the specified action is null, replaces it with an empty dummy action. An action
    /// which has been validated can be invoked without risking a NullReferenceException.
    /// </summary>
    public static void ValidateAction<T>(ref Action<T> action, string name="")
    {
        action = ValidateAction(action, name);
    }

    /// <summary>
    /// If the specified action is null, replaces it with an empty dummy action. An action
    /// which has been validated can be invoked without risking a NullReferenceException.
    /// </summary>
    public static Action<T> ValidateAction<T>(Action<T> action, string name="")
    {
        return (arg => {
            if (!string.IsNullOrEmpty(name)) Debug.Log(name + " -> " + arg);
            if (action != null) action(arg);
        });
    }

    /// <summary>
    /// Tests to check if the database is working properly.
    /// </summary>
    public static void RunTests()
    {
        OnlineDatabase db = new OnlineDatabase();
        
        db.Exists("test/does/not/exist", exists => {
            Debug.Assert(exists == false);
        });

        db.Push("test/data/key", "value", success => {
            Debug.Assert(success == true);
            db.Exists("test/data", exists => {
                Debug.Assert(exists == true);
                db.Exists("test/data/key", keyExists => {
                    Debug.Assert(keyExists == true);
                    db.Pull("test/data/key", result => {
                        Debug.Assert(result == "value");
                        db.Delete("test", deleted => {
                            Debug.Assert(deleted == true);
                            db.Exists("test", testExists => {
                                Debug.Assert(testExists == false);
                            });
                        });
                    });
                });
            });
        });
    }
}
