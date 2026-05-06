using UnityEngine;
using System.Collections.Generic;

public class CleanWireframe : MonoBehaviour
{
    [Header("Einstellungen")]
    public Color linienFarbe = Color.black;

    void Start()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null) return;

        Mesh mesh = mf.mesh;
        int[] triangles = mesh.triangles;

        // Speichert, wie oft eine Kante von Dreiecken benutzt wird
        Dictionary<Vector2Int, int> edgeCount = new Dictionary<Vector2Int, int>();

        for (int i = 0; i < triangles.Length; i += 3)
        {
            AddEdge(edgeCount, triangles[i], triangles[i + 1]);
            AddEdge(edgeCount, triangles[i + 1], triangles[i + 2]);
            AddEdge(edgeCount, triangles[i + 2], triangles[i]);
        }

        List<int> lineIndices = new List<int>();

        foreach (var kvp in edgeCount)
        {
            // Der Magie-Moment: In Unity sind harte Kanten am Modell technisch getrennt (Count = 1).
            // Nervige Diagonalen auf flachen Ebenen werden von zwei Dreiecken geteilt (Count = 2).
            // Wir zeichnen also nur Linien mit Count == 1!
            if (kvp.Value == 1)
            {
                lineIndices.Add(kvp.Key.x);
                lineIndices.Add(kvp.Key.y);
            }
        }

        // Neues Mesh nur für die reinen Linien erstellen
        Mesh wireMesh = new Mesh();
        wireMesh.vertices = mesh.vertices;
        wireMesh.SetIndices(lineIndices.ToArray(), MeshTopology.Lines, 0);

        // Ein leeres Objekt als Kind anlegen, das unsere sauberen Linien trägt
        GameObject wireObj = new GameObject("Saubere_Kanten");
        wireObj.transform.SetParent(transform, false);

        // Ganz leicht vergrößern, damit die Linien nicht im Modell verschwinden (Z-Fighting)
        wireObj.transform.localScale = new Vector3(1.002f, 1.002f, 1.002f);

        MeshFilter wireMf = wireObj.AddComponent<MeshFilter>();
        wireMf.mesh = wireMesh;

        MeshRenderer wireMr = wireObj.AddComponent<MeshRenderer>();

        // Kompatibel mit URP machen
        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlit == null) unlit = Shader.Find("Unlit/Color"); // Fallback

        Material wireMat = new Material(unlit);
        if (wireMat.HasProperty("_BaseColor"))
            wireMat.SetColor("_BaseColor", linienFarbe);
        else
            wireMat.color = linienFarbe;

        wireMr.material = wireMat;
    }

    void AddEdge(Dictionary<Vector2Int, int> dict, int v1, int v2)
    {
        // Immer den kleineren Index zuerst, damit hin und zurück als dieselbe Kante erkannt werden
        Vector2Int edge = new Vector2Int(Mathf.Min(v1, v2), Mathf.Max(v1, v2));
        if (dict.ContainsKey(edge))
            dict[edge]++;
        else
            dict[edge] = 1;
    }
}