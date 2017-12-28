using Firebase;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

class Startup : MonoBehaviour
{
    [SerializeField] Text m_status = null;

    async void Start()
    {
        // Check for Firebase dependencies
        DependencyStatus status;
        try { status = await FirebaseApp.CheckAndFixDependenciesAsync(); }
        catch (Exception e) { m_status.text = $"Dependency check failed: {e.Message}"; return; }
        if (status != DependencyStatus.Available) { m_status.text = $"Dependencies not available: {status.ToString()}"; return; }

        // Load the lobby scene
        SceneManager.LoadScene("Lobby");
    }
}
