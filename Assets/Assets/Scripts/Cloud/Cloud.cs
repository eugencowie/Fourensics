using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Unity.Editor;
using Google;
using System.Threading.Tasks;

interface ICloudObject
{
    void Create(Key key);
    Task Fetch(Key key);
}

static class Cloud
{
    public static FirebaseApp Firebase => FirebaseApp.DefaultInstance;
    public static FirebaseAuth Auth => FirebaseAuth.DefaultInstance;
    public static FirebaseDatabase Database => GetDatabase();
    public static GoogleSignIn Google => GoogleSignIn.DefaultInstance;

    static FirebaseDatabase GetDatabase()
    {
        Firebase.SetEditorDatabaseUrl("https://fourensics-app.firebaseio.com/");
        Firebase.SetEditorP12FileName("db-access-token.p12");
        Firebase.SetEditorServiceAccountEmail("fourensics-app@appspot.gserviceaccount.com");
        Firebase.SetEditorP12Password("notasecret");
        return FirebaseDatabase.DefaultInstance;
    }

    public static T Create<T>(Key key)
        where T : ICloudObject, new()
    {
        T obj = new T();
        obj.Create(key);
        return obj;
    }

    public static async Task<T> Fetch<T>(Key key)
        where T : ICloudObject, new()
    {
        T obj = new T();
        await obj.Fetch(key);
        return obj;
    }

    public static T Create<T>(string path, string id) where T : ICloudObject, new() => Create<T>(new Key(path, id));

    public static Task<T> Fetch<T>(string path, string id) where T : ICloudObject, new() => Fetch<T>(new Key(path, id));
}
