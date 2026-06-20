using UnityEngine;
using Unity.Cinemachine; // Cinemachine 3 namespace

public class CameraCollisionFix : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void FixCameraCollisionOnLoad()
    {
        SetupCameraCollision();
    }

    private static void SetupCameraCollision()
    {
        bool fixedCam = false;

        // Versuche CinemachineCamera (v3) oder VirtualCamera (v2) zu finden
        var cameras = FindObjectsOfType<MonoBehaviour>(); // Fallback um alle MonoBehaviours zu durchsuchen
        foreach (var cam in cameras)
        {
            string typeName = cam.GetType().Name;
            if (typeName == "CinemachineVirtualCamera" || typeName == "CinemachineFreeLook" || typeName == "CinemachineCamera")
            {
                // In Cinemachine 3 heißt es CinemachineDeoccluder, in v2 CinemachineCollider
                var collider = cam.GetComponent<CinemachineDeoccluder>();
                if (collider == null)
                {
                    collider = cam.gameObject.AddComponent<CinemachineDeoccluder>();
                    
                    // Alles außer Player und IgnoreRaycast kollidiert mit der Kamera
                    int playerLayer = LayerMask.NameToLayer("Player");
                    int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
                    
                    int mask = ~0; // Everything
                    if (playerLayer != -1) mask &= ~(1 << playerLayer);
                    if (ignoreRaycastLayer != -1) mask &= ~(1 << ignoreRaycastLayer);

                    int waterLayer = LayerMask.NameToLayer("Water");
                    if (waterLayer != -1) mask &= ~(1 << waterLayer);
                    int uiLayer = LayerMask.NameToLayer("UI");
                    if (uiLayer != -1) mask &= ~(1 << uiLayer);

                    collider.CollideAgainst = mask;
                    collider.MinimumDistanceFromTarget = 0.5f;

                    fixedCam = true;
                    Debug.Log($"<color=green>[Camera Fix]</color> CinemachineDeoccluder zu {cam.name} hinzugefügt.");
                }
            }
        }

        if (!fixedCam)
        {
            Debug.Log("[Camera Fix] Keine Kameras gefunden oder Kameras haben bereits Deoccluder.");
        }
    }
}
