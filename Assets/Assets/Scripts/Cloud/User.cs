using Firebase;
using Firebase.Auth;
using Firebase.Messaging;
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
    public CloudNode NotificationToken { get; private set; }
    public CloudNode Name { get; private set; }
    public CloudNode Lobby { get; private set; }

    public string Id => Key.Id;

    void ICloudObject.Create(Key key)
    {
        Key = key;
        Type = CloudNode<long>.Create(Key.Child("type"));
        Token = CloudNode.Create(Key.Child("token"));
        NotificationToken = CloudNode.Create(Key.Child("notification-token"));
        Name = CloudNode.Create(Key.Child("name"));
        Lobby = CloudNode.Create(Key.Child("lobby"));
    }

    async Task ICloudObject.Fetch(Key key)
    {
        Key = key;
        Type = await CloudNode<long>.Fetch(Key.Child("type"));
        Token = await CloudNode.Fetch(Key.Child("token"));
        NotificationToken = await CloudNode.Fetch(Key.Child("notification-token"));
        Name = await CloudNode.Fetch(Key.Child("name"));
        Lobby = await CloudNode.Fetch(Key.Child("lobby"));
    }

    #region Push notification methods

    void OnTokenReceived(object sender, TokenReceivedEventArgs token)
    {
        Debug.Log("Received registration token: " + token.Token);
        NotificationToken.Value = token.Token;
    }

    void OnMessageReceived(object sender, MessageReceivedEventArgs e)
    {
        Debug.Log("Received a new message from: " + e.Message.From);
    }

    #endregion

    #region Static methods

    static User m_instance = null;

    public async static Task SignInWithGoogle()
    {
        // Check for Firebase dependencies
        DependencyStatus status = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (status != DependencyStatus.Available) { throw new Exception("Unable to satisfy dependencies."); }

        // Set up Google sign in service
        GoogleSignIn.Configuration = new GoogleSignInConfiguration
        {
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
        m_instance = await Cloud.Fetch<User>(new Key("users").Child(SystemInfo.deviceUniqueIdentifier));
        m_instance.Name.Value = firebaseUser.DisplayName;
        m_instance.Type.Value = (long)UserType.Google;
        m_instance.Token.Value = firebaseUser.UserId;

        // Set up push notifications
        FirebaseMessaging.TokenReceived += m_instance.OnTokenReceived;
        FirebaseMessaging.MessageReceived += m_instance.OnMessageReceived;
    }

    public async static Task SignInAsGuest()
    {
        // Check for Firebase dependencies
        DependencyStatus status = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (status != DependencyStatus.Available) { throw new Exception("Unable to satisfy dependencies."); }

        // Authenticate as a guest user
        FirebaseAuth.GetAuth(Cloud.Firebase);
        FirebaseUser firebaseUser = await Cloud.Auth.SignInAnonymouslyAsync();

        // Fetch user from the cloud using Firebase user id
        m_instance = await Cloud.Fetch<User>(new Key("users").Child(SystemInfo.deviceUniqueIdentifier));
        m_instance.Name.Value = "Guest";
        m_instance.Type.Value = (long)UserType.Device;
        m_instance.Token.Value = firebaseUser.UserId;

        // Set up push notifications
        FirebaseMessaging.TokenReceived += m_instance.OnTokenReceived;
        FirebaseMessaging.MessageReceived += m_instance.OnMessageReceived;
    }

    public async static Task<User> Get()
    {
        if (m_instance == null)
        {
            // Check for Firebase dependencies
            DependencyStatus status = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (status != DependencyStatus.Available) { throw new Exception("Unable to satisfy dependencies."); }

            // Fetch user from the cloud using Firebase user id
            m_instance = await Cloud.Fetch<User>(new Key("users").Child(SystemInfo.deviceUniqueIdentifier));

            // Check if device has signed-in before
            if (m_instance.Type.Value.HasValue)
            {
                // Use whichever sign-in type was used before
                if (m_instance.Type.Value == (long)UserType.Google)
                    await SignInWithGoogle();
                else
                    await SignInAsGuest();
            }
            else
                throw new Exception();
        }

        return m_instance;
    }

    #endregion
}
