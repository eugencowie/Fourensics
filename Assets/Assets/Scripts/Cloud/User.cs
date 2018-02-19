using Firebase;
using Firebase.Auth;
using Google;
using System;
using System.Threading.Tasks;
using UnityEngine;

class User : ICloudObject
{
    public Key Key { get; private set; }
    public CloudNode Name { get; private set; }
    public CloudNode Lobby { get; private set; }

    public string Id => Key.Id;

    void ICloudObject.Create(Key key)
    {
        Key = key;
        Name = CloudNode.Create(Key.Child("name"));
        Lobby = CloudNode.Create(Key.Child("lobby"));
    }

    async Task ICloudObject.Fetch(Key key)
    {
        Key = key;
        Name = await CloudNode.Fetch(Key.Child("name"));
        Lobby = await CloudNode.Fetch(Key.Child("lobby"));
    }

    static User m_instance = null;

    public async static Task<User> Get()
    {
        if (m_instance == null)
        {
            // Check for Firebase dependencies
            DependencyStatus status = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (status != DependencyStatus.Available) { throw new Exception("Unable to satisfy dependencies."); }

            if (Application.isEditor)
            {
                // Fetch user from the cloud using device id
                m_instance = await Cloud.Fetch<User>("users", $"dev-{SystemInfo.deviceUniqueIdentifier}");
                m_instance.Name.Value = $"Dev #{SystemInfo.deviceUniqueIdentifier.Substring(0, 7)}";
            }
            else
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
                m_instance = await Cloud.Fetch<User>("users", firebaseUser.UserId);
                m_instance.Name.Value = firebaseUser.DisplayName;
            }
        }

        return m_instance;
    }
}
