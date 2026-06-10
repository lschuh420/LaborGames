using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MechSetupWizard : MonoBehaviour
{
    [Header("Settings")]
    public float armorMultiplier = 0.5f;
    public float weakSpotMultiplier = 2.0f;
    public float legMultiplier = 1.0f;

    [Header("Naming Conventions")]
    public string bodyKeyword = "Body";
    public string legKeyword = "Leg";
    public string footTipKeyword = "_Tip";
    public string rootKeyword = "_Root";

    [ContextMenu("Auto Setup Boss Components")]
    public void SetupBoss()
    {
        MechBossHealth mainHealth = GetComponent<MechBossHealth>();
        if (mainHealth == null) mainHealth = gameObject.AddComponent<MechBossHealth>();

        StepManager stepManager = GetComponentInChildren<StepManager>();
        BodyAdaptation bodyAdapt = GetComponentInChildren<BodyAdaptation>();

        // Alle Collider durchsuchen
        Collider[] allColliders = GetComponentsInChildren<Collider>(true);
        int routerCount = 0;
        int legHealthCount = 0;

        foreach (Collider col in allColliders)
        {
            GameObject obj = col.gameObject;
            string n = obj.name;

            // 1. DamageRouter (Hitbox) hinzufügen
            DamageRouter router = obj.GetComponent<DamageRouter>();
            if (router == null) router = obj.AddComponent<DamageRouter>();
            router.mainHealth = mainHealth;
            routerCount++;

            // Multiplier setzen
            if (n.Contains(bodyKeyword)) router.damageMultiplier = armorMultiplier;
            else if (n.Contains(legKeyword)) router.damageMultiplier = legMultiplier;

            // 2. Beine speziell behandeln
            if (n.Contains(legKeyword))
            {
                // Den Leg_XX_Root finden (geht die Hierarchie hoch)
                Transform current = obj.transform;
                MechLegHealth legHealth = null;
                
                while (current != null && current != transform)
                {
                    if (current.name.Contains(legKeyword) && current.name.Contains(rootKeyword))
                    {
                        legHealth = current.GetComponent<MechLegHealth>();
                        if (legHealth == null) legHealth = current.gameObject.AddComponent<MechLegHealth>();
                        break;
                    }
                    current = current.parent;
                }

                if (legHealth != null)
                {
                    legHealth.bossMainHealth = mainHealth;
                    router.legHealth = legHealth;

                    // LegIK finden (meist auf dem Root)
                    if (legHealth.legIK == null) legHealth.legIK = legHealth.GetComponent<LegIK>();

                    // FootTip finden (sucht in den Kindern nach "_Tip")
                    if (legHealth.footTip == null)
                    {
                        foreach (Transform child in legHealth.GetComponentsInChildren<Transform>())
                        {
                            if (child.name.Contains(footTipKeyword))
                            {
                                legHealth.footTip = child;
                                break;
                            }
                        }
                    }

                    // Mesh finden (erstes MeshRenderer Kind)
                    if (legHealth.legMesh == null)
                    {
                        MeshRenderer mr = legHealth.GetComponentInChildren<MeshRenderer>();
                        if (mr != null) legHealth.legMesh = mr.gameObject;
                    }
                    
                    legHealthCount++;
                }
            }
        }

        Debug.Log($"<color=green>CÜSSS! Wizard fertig. {routerCount} Hitboxen verteilt und Beine verlinkt.</color>");
        EditorUtility.SetDirty(gameObject);
    }
}

[CustomEditor(typeof(MechSetupWizard))]
public class MechSetupWizardEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        MechSetupWizard wizard = (MechSetupWizard)target;
        GUILayout.Space(20);
        if (GUILayout.Button("AUTO SETUP BOSS (KOCHEN STARTEN)", GUILayout.Height(40)))
        {
            wizard.SetupBoss();
        }
    }
}
