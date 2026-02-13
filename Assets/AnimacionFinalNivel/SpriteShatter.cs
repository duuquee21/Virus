using UnityEngine;

public class SpriteShatter : MonoBehaviour
{
    [Range(3, 50)] public int totalFragments = 12;
    public float explosionForce = 400f;

    [ContextMenu("Shatter Sprite Radial")]
    public void Shatter()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;

        Sprite sprite = sr.sprite;
        int sortingID = sr.sortingLayerID;
        int sortingOrder = sr.sortingOrder;

        Vector3 spawnPosition = transform.position;
        Quaternion spawnRotation = transform.rotation;
        Vector3 spawnScale = transform.localScale;

        sr.enabled = false;

        float angleStep = 360f / totalFragments;
        for (int i = 0; i < totalFragments; i++)
        {
            CreateRadialFragment(i * angleStep, (i + 1) * angleStep, sprite, sortingID, sortingOrder, spawnPosition, spawnRotation, spawnScale);
        }
    }

    void CreateRadialFragment(float startAngle, float endAngle, Sprite s, int sID, int sOrder, Vector3 pos, Quaternion rot, Vector3 scale)
    {
        GameObject fragment = new GameObject($"Shard_{startAngle}");
        MeshFilter mf = fragment.AddComponent<MeshFilter>();
        MeshRenderer mr = fragment.AddComponent<MeshRenderer>();

        Mesh mesh = CreateTriangleMesh(startAngle, endAngle, s);

        // --- NUEVA LÓGICA: CENTRO DE MASA POR ALPHA ---
        Vector3 alphaCenter = GetAlphaCenterOfMass(mesh, s);

        // Ajustamos los vértices para que el pivote sea el centro del Alpha
        Vector3[] shiftedVertices = mesh.vertices;
        for (int i = 0; i < shiftedVertices.Length; i++)
        {
            shiftedVertices[i] -= alphaCenter;
        }
        mesh.vertices = shiftedVertices;
        mesh.RecalculateBounds();

        mf.mesh = mesh;
        mr.sortingLayerID = sID;
        mr.sortingOrder = sOrder;
        mr.material = new Material(Shader.Find("Sprites/Default"));
        mr.material.mainTexture = s.texture;

        // Posicionamiento compensado
        fragment.transform.rotation = rot;
        fragment.transform.localScale = scale;
        fragment.transform.position = pos + rot * Vector3.Scale(alphaCenter, scale);

        // Física
        Rigidbody2D rb = fragment.AddComponent<Rigidbody2D>();
        fragment.AddComponent<PolygonCollider2D>();

        float midAngleRad = (startAngle + endAngle) / 2f * Mathf.Deg2Rad;
        Vector2 forceDir = new Vector2(Mathf.Cos(midAngleRad), Mathf.Sin(midAngleRad));
        rb.AddForce(forceDir * explosionForce);
        rb.AddTorque(Random.Range(-50f, 50f));

        Destroy(fragment, 3f);
    }

    Vector3 GetAlphaCenterOfMass(Mesh mesh, Sprite s)
    {
        Vector3 centroid = Vector3.zero;
        float totalAlpha = 0;

        // Escaneamos el área del triángulo en la textura
        // Para optimizar, usamos los bounds de la malla (en coordenadas locales)
        Bounds b = mesh.bounds;
        float ppu = s.pixelsPerUnit;

        // Convertimos bounds locales a coordenadas de textura
        Rect r = s.textureRect;

        // Muestreamos la textura en el área del fragmento
        // Nota: La textura debe ser Read/Write Enabled en el Inspector
        for (float x = b.min.x; x < b.max.x; x += 1f / ppu)
        {
            for (float y = b.min.y; y < b.max.y; y += 1f / ppu)
            {
                // Convertir posición local a UV
                float normX = (x / (s.bounds.size.x / 2f) + 1f) / 2f;
                float normY = (y / (s.bounds.size.y / 2f) + 1f) / 2f;

                int texX = (int)(r.x + normX * r.width);
                int texY = (int)(r.y + normY * r.height);

                Color pixel = s.texture.GetPixel(texX, texY);
                if (pixel.a > 0.1f) // Solo si no es transparente
                {
                    centroid += new Vector3(x, y, 0);
                    totalAlpha++;
                }
            }
        }

        // Si el fragmento es totalmente transparente, usamos el centro geométrico por defecto
        if (totalAlpha == 0)
        {
            foreach (var v in mesh.vertices) centroid += v;
            return centroid / 3f;
        }

        return centroid / totalAlpha;
    }

    // [Tu función CreateTriangleMesh se mantiene igual que antes]
    Mesh CreateTriangleMesh(float startAngle, float endAngle, Sprite s)
    {
        Mesh mesh = new Mesh();
        float w = s.bounds.extents.x;
        float h = s.bounds.extents.y;

        Vector3[] vertices = new Vector3[3];
        vertices[0] = Vector3.zero;

        Vector3 GetRectEdgePoint(float angleDeg, float width, float height)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            float factorX = width / Mathf.Max(Mathf.Abs(cos), 0.0001f);
            float factorY = height / Mathf.Max(Mathf.Abs(sin), 0.0001f);
            float minFactor = Mathf.Min(factorX, factorY);
            return new Vector3(cos * minFactor, sin * minFactor, 0);
        }

        vertices[1] = GetRectEdgePoint(startAngle, w, h);
        vertices[2] = GetRectEdgePoint(endAngle, w, h);

        Rect r = s.textureRect;
        float tw = s.texture.width;
        float th = s.texture.height;

        Vector2 GetUVPoint(Vector3 vertex, Rect rect, float texW, float texH)
        {
            float normX = (vertex.x / s.bounds.extents.x + 1f) / 2f;
            float normY = (vertex.y / s.bounds.extents.y + 1f) / 2f;
            return new Vector2((rect.x + normX * rect.width) / texW, (rect.y + normY * rect.height) / texH);
        }

        mesh.vertices = vertices;
        mesh.uv = new Vector2[] {
            new Vector2(r.center.x / tw, r.center.y / th),
            GetUVPoint(vertices[1], r, tw, th),
            GetUVPoint(vertices[2], r, tw, th)
        };
        mesh.triangles = new int[] { 0, 1, 2 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}