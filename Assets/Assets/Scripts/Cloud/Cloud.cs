using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Unity.Editor;
using Google;

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
}
