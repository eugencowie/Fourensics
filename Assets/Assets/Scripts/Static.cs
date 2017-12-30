using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Unity.Editor;
using Google;

static class Static
{
    public static FirebaseApp FirebaseApp => FirebaseApp.DefaultInstance;
    public static FirebaseAuth FirebaseAuth => FirebaseAuth.DefaultInstance;
    public static FirebaseDatabase FirebaseDatabase => GetFirebaseDatabase();

    public static GoogleSignIn GoogleSignIn => GoogleSignIn.DefaultInstance;

    static FirebaseDatabase GetFirebaseDatabase()
    {
        FirebaseApp.SetEditorDatabaseUrl("https://fourensics-app.firebaseio.com/");
        FirebaseApp.SetEditorP12FileName("db-access-token.p12");
        FirebaseApp.SetEditorServiceAccountEmail("fourensics-app@appspot.gserviceaccount.com");
        FirebaseApp.SetEditorP12Password("notasecret");
        return FirebaseDatabase.DefaultInstance;
    }
}
