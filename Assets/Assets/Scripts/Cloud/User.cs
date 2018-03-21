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

    async static Task<User> Authenticate()
    {
        // Initialise Firebase authentication
        FirebaseAuth.GetAuth(Cloud.Firebase);

        if (m_instance.Type.Value == (long)UserType.Google)
        {
            // Authenticate as a Google user
            Credential credential = GoogleAuthProvider.GetCredential(m_instance.Token.Value, null);
            FirebaseUser user = await Cloud.Auth.SignInWithCredentialAsync(credential);
        }
        else
        {
            // Authenticate as a guest user
            Credential credential = EmailAuthProvider.GetCredential(m_instance.Token.Value, m_instance.Token.Value);
            FirebaseUser user = await Cloud.Auth.SignInWithCredentialAsync(credential);
        }

        return m_instance;
    }

    public async static Task<User> Create(UserType type)
    {
        if (type == UserType.Google)
        {
            // Set up Google sign in service
            GoogleSignIn.Configuration = new GoogleSignInConfiguration {
                WebClientId = "1066471497679-ceos2isgtrb36rctu7coq1da2igs922r.apps.googleusercontent.com",
                RequestIdToken = true
            };

            // Sign in using Google
            GoogleSignInUser googleUser = await Cloud.Google.SignIn();

            // Create database object
            Key userKey = new Key("users").Child(SystemInfo.deviceUniqueIdentifier);
            m_instance = Cloud.Create<User>(userKey);
            m_instance.Type.Value = (long)UserType.Google;
            m_instance.Token.Value = googleUser.IdToken;
            m_instance.Name.Value = googleUser.DisplayName;
        }
        else
        {
            // Create database object
            Key userKey = new Key("users").Child(SystemInfo.deviceUniqueIdentifier);
            m_instance = Cloud.Create<User>(userKey);
            m_instance.Type.Value = (long)UserType.Device;
            m_instance.Token.Value = userKey.Id;
            m_instance.Name.Value = "Guest";
        }

        await Authenticate();

        return m_instance;
    }

    public async static Task<User> Get()
    {
        if (m_instance == null)
        {
            // Check for Firebase dependencies
            DependencyStatus status = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (status != DependencyStatus.Available) { throw new Exception("Unable to satisfy dependencies."); }

            // Fetch user from the cloud using unique device id
            m_instance = await Cloud.Fetch<User>(new Key("users").Child(SystemInfo.deviceUniqueIdentifier));

            if (!m_instance.Type.Value.HasValue || string.IsNullOrWhiteSpace(m_instance.Token.Value))
            {
                throw new Exception();
            }
            else if (m_instance.Type.Value == (long)UserType.Google)
            {
                await Authenticate();
            }
            else
            {
                await Authenticate();
            }
        }

        return m_instance;
    }
}
