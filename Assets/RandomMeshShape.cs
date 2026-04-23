using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class RandomMeshShape : MonoBehaviour
{
    [Header("Forma base")]
    public float width = 1f;
    public float height = 1f;
    public float depth = 0.2f;
    public int subDiv = 4;

    [Header("Irregularidad (igual que el túnel)")]
    public float noiseScale = 1.2f;
    public float noiseStrength = 0.25f;

    [Header("Semilla")]
    public bool randomSeedOnStart = true;
    public float seed = 0f;

    void Start()
    {
        if (randomSeedOnStart)
            seed = Random.Range(0f, 1000f);

        GetComponent<MeshFilter>().mesh = BuildMesh();
    }

    // Público para que ShatterOnDestroy pueda leer el depth actual
    public float Depth => depth;

    private Mesh BuildMesh()
    {
        int vertsPerSide = subDiv + 1;
        int faceVerts = vertsPerSide * vertsPerSide;
        float halfDepth = depth * 0.5f;

        // Cara frontal con ruido Perlin
        Vector3[] frontVerts = new Vector3[faceVerts];
        Vector2[] frontUVs = new Vector2[faceVerts];

        for (int y = 0; y < vertsPerSide; y++)
        {
            for (int x = 0; x < vertsPerSide; x++)
            {
                float tx = (float)x / subDiv;
                float ty = (float)y / subDiv;

                float bx = (tx - 0.5f) * width;
                float by = (ty - 0.5f) * height;

                float noiseX = 0f, noiseY = 0f;

                // Solo vértices interiores se deforman — los bordes se quedan rectos
                // para que las caras laterales encajen bien
                if (x > 0 && x < subDiv && y > 0 && y < subDiv)
                {
                    float n1 = Mathf.PerlinNoise(seed + tx * noiseScale, seed + ty * noiseScale);
                    float n2 = Mathf.PerlinNoise(seed + ty * noiseScale + 5f, seed + tx * noiseScale + 5f);
                    noiseX = (n1 - 0.5f) * 2f * noiseStrength * width;
                    noiseY = (n2 - 0.5f) * 2f * noiseStrength * height;
                }

                frontVerts[y * vertsPerSide + x] = new Vector3(bx + noiseX, by + noiseY, -halfDepth);
                frontUVs[y * vertsPerSide + x] = new Vector2(tx, ty);
            }
        }

        // Cara trasera
        Vector3[] backVerts = new Vector3[faceVerts];
        Vector2[] backUVs = new Vector2[faceVerts];
        for (int i = 0; i < faceVerts; i++)
        {
            backVerts[i] = new Vector3(frontVerts[i].x, frontVerts[i].y, halfDepth);
            backUVs[i] = frontUVs[i];
        }

        // Perímetro para caras laterales
        System.Collections.Generic.List<Vector2Int> perim = new System.Collections.Generic.List<Vector2Int>();
        for (int x = 0; x < subDiv; x++) perim.Add(new Vector2Int(x, 0));
        for (int y = 0; y < subDiv; y++) perim.Add(new Vector2Int(subDiv, y));
        for (int x = subDiv; x > 0; x--) perim.Add(new Vector2Int(x, subDiv));
        for (int y = subDiv; y > 0; y--) perim.Add(new Vector2Int(0, y));

        int perimCount = perim.Count;
        Vector3[] sideVerts = new Vector3[perimCount * 4];
        Vector2[] sideUVs = new Vector2[perimCount * 4];

        for (int i = 0; i < perimCount; i++)
        {
            int next = (i + 1) % perimCount;
            Vector3 a = frontVerts[perim[i].y * vertsPerSide + perim[i].x];
            Vector3 b = frontVerts[perim[next].y * vertsPerSide + perim[next].x];
            Vector3 c = backVerts[perim[next].y * vertsPerSide + perim[next].x];
            Vector3 d = backVerts[perim[i].y * vertsPerSide + perim[i].x];

            int vi = i * 4;
            sideVerts[vi] = a; sideVerts[vi + 1] = b; sideVerts[vi + 2] = c; sideVerts[vi + 3] = d;
            float u0 = (float)i / perimCount, u1 = (float)(i + 1) / perimCount;
            sideUVs[vi] = new Vector2(u0, 0); sideUVs[vi + 1] = new Vector2(u1, 0);
            sideUVs[vi + 2] = new Vector2(u1, 1); sideUVs[vi + 3] = new Vector2(u0, 1);
        }

        // Ensamblar
        int totalVerts = faceVerts * 2 + perimCount * 4;
        Vector3[] allVerts = new Vector3[totalVerts];
        Vector2[] allUVs = new Vector2[totalVerts];
        System.Array.Copy(frontVerts, 0, allVerts, 0, faceVerts);
        System.Array.Copy(backVerts, 0, allVerts, faceVerts, faceVerts);
        System.Array.Copy(sideVerts, 0, allVerts, faceVerts * 2, perimCount * 4);
        System.Array.Copy(frontUVs, 0, allUVs, 0, faceVerts);
        System.Array.Copy(backUVs, 0, allUVs, faceVerts, faceVerts);
        System.Array.Copy(sideUVs, 0, allUVs, faceVerts * 2, perimCount * 4);

        System.Collections.Generic.List<int> tris = new System.Collections.Generic.List<int>();

        // Cara frontal
        for (int y = 0; y < subDiv; y++)
            for (int x = 0; x < subDiv; x++)
            {
                int bl = y * vertsPerSide + x, br = bl + 1, tl = bl + vertsPerSide, tr = tl + 1;
                tris.Add(bl); tris.Add(tl); tris.Add(tr);
                tris.Add(bl); tris.Add(tr); tris.Add(br);
            }

        // Cara trasera (invertida)
        for (int y = 0; y < subDiv; y++)
            for (int x = 0; x < subDiv; x++)
            {
                int bl = faceVerts + y * vertsPerSide + x, br = bl + 1, tl = bl + vertsPerSide, tr = tl + 1;
                tris.Add(bl); tris.Add(tr); tris.Add(tl);
                tris.Add(bl); tris.Add(br); tris.Add(tr);
            }

        // Caras laterales
        for (int i = 0; i < perimCount; i++)
        {
            int vi = faceVerts * 2 + i * 4;
            tris.Add(vi); tris.Add(vi + 1); tris.Add(vi + 2);
            tris.Add(vi); tris.Add(vi + 2); tris.Add(vi + 3);
        }

        Mesh mesh = new Mesh();
        mesh.name = "RandomShape";
        mesh.vertices = allVerts;
        mesh.uv = allUVs;
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();
        return mesh;
    }
}