using Firebase;
using Firebase.Database;
using Firebase.Unity.Editor;

static class Static
{
    public static FirebaseApp FirebaseApp => FirebaseApp.DefaultInstance;
    public static FirebaseDatabase FirebaseDatabase => GetFirebaseDatabase();

    static FirebaseDatabase GetFirebaseDatabase()
    {
        FirebaseApp.SetEditorDatabaseUrl("https://fourensics-app.firebaseio.com/");
        FirebaseApp.SetEditorP12FileName("db-access-token.p12");
        FirebaseApp.SetEditorServiceAccountEmail("fourensics-app@appspot.gserviceaccount.com");
        FirebaseApp.SetEditorP12Password("notasecret");
        return FirebaseDatabase.DefaultInstance;
    }
}
