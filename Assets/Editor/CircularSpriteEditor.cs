using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public class CircularSpriteEditor : EditorWindow
{
    public Sprite targetSprite;
    public int rings = 3;
    public int segments = 64;

    [MenuItem("Tools/Circular Sprite Generator")]
    public static void ShowWindow()
    {
        GetWindow<CircularSpriteEditor>("Generador Circular");
    }

    void OnGUI()
    {
        targetSprite = (Sprite)EditorGUILayout.ObjectField("Sprite a procesar", targetSprite, typeof(Sprite), false);
        rings = EditorGUILayout.IntSlider("Anillos", rings, 1, 50);
        segments = EditorGUILayout.IntSlider("Segmentos", segments, 3, 1024); // ¡Hasta 1024!

        if (GUILayout.Button("Generar Malla Circular") && targetSprite != null)
        {
            ApplyCircularMesh();
        }
    }

    void ApplyCircularMesh()
    {
        string path = AssetDatabase.GetAssetPath(targetSprite);
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dataProvider = factory.GetSpriteEditorDataProviderFromObject(AssetDatabase.LoadMainAssetAtPath(path));
        dataProvider.InitSpriteEditorDataProvider();

        // Obtenemos el rect real del sprite desde el provider para evitar desvíos
        var spriteRects = dataProvider.GetDataProvider<ISpriteEditorDataProvider>().GetSpriteRects();
        var spriteGuid = targetSprite.GetSpriteID();
        var currentRect = spriteRects.FirstOrDefault(s => s.spriteID == spriteGuid);

        if (currentRect == null)
        {
            Debug.LogError("No se pudo encontrar el Rect del Sprite.");
            return;
        }

        var meshProvider = dataProvider.GetDataProvider<ISpriteMeshDataProvider>();

        List<Vertex2DMetaData> vertices = new List<Vertex2DMetaData>();
        List<Vector2Int> edges = new List<Vector2Int>();
        List<int> indices = new List<int>();

        // EL TRUCO: En el Sprite Editor, las posiciones de los vértices 
        // son RELATIVAS al Rect del sprite, no al Pivot.
        // El centro absoluto es la mitad del ancho y el alto.
        Vector2 localCenter = new Vector2(currentRect.rect.width / 2f, currentRect.rect.height / 2f);

        // 1. Vértice Central
        vertices.Add(new Vertex2DMetaData { position = localCenter });

        float radiusX = currentRect.rect.width / 2f;
        float radiusY = currentRect.rect.height / 2f;

        // 2. Generar Anillos
        for (int r = 1; r <= rings; r++)
        {
            float t = (float)r / rings;
            float currRX = radiusX * t;
            float currRY = radiusY * t;

            for (int s = 0; s < segments; s++)
            {
                float angle = s * Mathf.PI * 2 / segments;
                // Posición calculada desde el centro local
                Vector2 pos = new Vector2(
                    Mathf.Cos(angle) * currRX,
                    Mathf.Sin(angle) * currRY
                ) + localCenter;

                vertices.Add(new Vertex2DMetaData { position = pos });
            }
        }

        // 3. Generar Triángulos (Indices) y Bordes
        for (int r = 1; r <= rings; r++)
        {
            int currentRingStart = 1 + (r - 1) * segments;
            int prevRingStart = 1 + (r - 2) * segments;

            for (int s = 0; s < segments; s++)
            {
                int nextS = (s + 1) % segments;
                int vCurrent = currentRingStart + s;
                int vNext = currentRingStart + nextS;

                // Bordes externos del anillo actual
                edges.Add(new Vector2Int(vCurrent, vNext));

                if (r == 1)
                {
                    // Triángulos del centro al primer anillo
                    indices.Add(0);
                    indices.Add(vNext);
                    indices.Add(vCurrent);
                }
                else
                {
                    // Quads entre anillos
                    int vPrev = prevRingStart + s;
                    int vPrevNext = prevRingStart + nextS;

                    // Triángulo A
                    indices.Add(vCurrent);
                    indices.Add(vNext);
                    indices.Add(vPrev);

                    // Triángulo B
                    indices.Add(vPrev);
                    indices.Add(vNext);
                    indices.Add(vPrevNext);
                }
            }
        }

        // 4. Aplicar y Guardar
        meshProvider.SetVertices(spriteGuid, vertices.ToArray());
        meshProvider.SetEdges(spriteGuid, edges.ToArray());
        meshProvider.SetIndices(spriteGuid, indices.ToArray());

        dataProvider.Apply();

        var assetImporter = dataProvider.targetObject as AssetImporter;
        assetImporter.SaveAndReimport();

        Debug.Log($"Malla circular aplicada. Centro en: {localCenter}. Vértices: {vertices.Count}");
    }
}