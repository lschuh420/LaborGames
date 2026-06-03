using UnityEngine;

public class AudioListenerFixer : MonoBehaviour
{
    private void Awake()
    {
        // Wir suchen alle AudioListener in der Szene
        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);

        if (listeners.Length > 1)
        {
            Debug.Log($"<color=cyan>[AudioFixer] {listeners.Length} AudioListener gefunden. Ich räume auf...</color>");

            // Wir behalten den Listener auf der MainCamera und schalten alle anderen aus
            foreach (AudioListener listener in listeners)
            {
                if (listener.gameObject.CompareTag("MainCamera"))
                {
                    continue; // MainCamera lassen wir in Ruhe
                }

                listener.enabled = false;
                Debug.Log($"<color=gray>[AudioFixer] Redundanter Listener auf '{listener.gameObject.name}' wurde deaktiviert.</color>");
            }
        }
    }
}
