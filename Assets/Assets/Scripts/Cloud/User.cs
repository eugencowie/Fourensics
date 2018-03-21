using Firebase;
using Firebase.Auth;
using Google;
using System;
using System.Threading.Tasks;
using UnityEngine;

enum UserType { Google, Device }

class User : ICloudObject
{
    public Key Key { get; private set; }
    public CloudNode<long> Type { get; private set; }
    public CloudNode Token { get; private set; }
    public CloudNode Name { get; private set; }
    public CloudNode Lobby { get; private set; }

    public string Id => Key.Id;

    void ICloudObject.Create(Key key)
    {
        Key = key;
        Type = CloudNode<long>.Create(Key.Child("type"));
        Token = CloudNode.Create(Key.Child("token"));
        Name = CloudNode.Create(Key.Child("name"));
        Lobby = CloudNode.Create(Key.Child("lobby"));
    }

    async Task ICloudObject.Fetch(Key key)
    {
        Key = key;
        Type = await CloudNode<long>.Fetch(Key.Child("type"));
        Token = await CloudNode.Fetch(Key.Child("token"));
        Name = await CloudNode.Fetch(Key.Child("name"));
        Lobby = await CloudNode.Fetch(Key.Child("lobby"));
    }

    static User m_instance = null;
    
    public async static Task SignInWithGoogle()
    {
        // Set up Google sign in service
        GoogleSignIn.Configuration = new GoogleSignInConfiguration {
            WebClientId = "1066471497679-ceos2isgtrb36rctu7coq1da2igs922r.apps.googleusercontent.com",
            RequestIdToken = true
        };

        // Sign in using Google
        GoogleSignInUser googleUser = await Cloud.Google.SignIn();

        // Authenticate as a Google user
        FirebaseAuth.GetAuth(Cloud.Firebase);
        Credential credential = GoogleAuthProvider.GetCredential(googleUser.IdToken, null);
        FirebaseUser firebaseUser = await Cloud.Auth.SignInWithCredentialAsync(credential);

        // Fetch user from the cloud using Firebase user id
        m_instance = await Cloud.Fetch<User>(new Key("users").Child(firebaseUser.UserId));
        m_instance.Name.Value = firebaseUser.DisplayName;
    }

    public async static Task SignInAsGuest()
    {
        // Authenticate as a guest user
        FirebaseAuth.GetAuth(Cloud.Firebase);
        FirebaseUser firebaseUser = await Cloud.Auth.SignInAnonymouslyAsync();

        // Fetch user from the cloud using Firebase user id
        m_instance = await Cloud.Fetch<User>(new Key("users").Child(firebaseUser.UserId));
        m_instance.Name.Value = "Guest";
    }

    public static User Get()
    {
        if (m_instance == null)
            throw new Exception();

        return m_instance;
    }
}
