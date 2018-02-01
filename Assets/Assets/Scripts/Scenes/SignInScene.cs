using Firebase;
using Firebase.Auth;
using Google;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

class SignInScene : MonoBehaviour
{
    public static User User { get; private set; }

    [SerializeField] Text m_status = null;

    async void Start()
    {
        // Check for Firebase dependencies
        DependencyStatus status;
        try { status = await FirebaseApp.CheckAndFixDependenciesAsync(); }
        catch (Exception e) { m_status.text = $"Dependency check failed: {e.Message}"; return; }
        if (status != DependencyStatus.Available) { m_status.text = $"Dependencies not available: {status.ToString()}"; return; }

        if (Application.isEditor)
        {
            // Fetch user from the cloud using device id
            User = await User.Fetch($"dev-{SystemInfo.deviceUniqueIdentifier}");
            User.Name.Value = $"Dev #{SystemInfo.deviceUniqueIdentifier.Substring(0, 7)}";
        }
        else
        {
            // Set up Google sign in service
            GoogleSignIn.Configuration = new GoogleSignInConfiguration {
                WebClientId = "1066471497679-ceos2isgtrb36rctu7coq1da2igs922r.apps.googleusercontent.com",
                RequestIdToken = true
            };

            // Sign in using Google
            GoogleSignInUser googleUser;
            try { googleUser = await Cloud.Google.SignIn(); }
            catch (Exception e) { m_status.text = $"Google sign in failed: {e.Message}"; return; }

            // Authenticate as a Google user
            FirebaseUser firebaseUser;
            FirebaseAuth.GetAuth(Cloud.Firebase);
            Credential credential = GoogleAuthProvider.GetCredential(googleUser.IdToken, null);
            try { firebaseUser = await Cloud.Auth.SignInWithCredentialAsync(credential); }
            catch (Exception e) { m_status.text = $"Authentication failed: {e.Message}"; return; }

            // Fetch user from the cloud using Firebase user id
            User = await User.Fetch(firebaseUser.UserId);
            User.Name.Value = firebaseUser.DisplayName;
        }

        // Load the lobby scene
        SceneManager.LoadScene("Lobby");
    }
}
