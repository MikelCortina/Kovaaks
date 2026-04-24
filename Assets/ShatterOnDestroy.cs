using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class ShatterOnDestroy : MonoBehaviour
{
    [Header("Fragmentación")]
    public int fragmentCount = 12;

    [Header("Irregularidad")]
    public float noiseScale = 1.2f;
    public float noiseStrength = 0.3f;

    [Header("Física de los fragmentos")]
    public float explosionForce = 5f;
    public float explosionTorque = 3f;
    public float fragmentLifetime = 1.8f;
    public float shrinkDelay = 0.4f;

    [Header("Material")]
    public Material fragmentMaterial;

    [Header("Grosor de los fragmentos")]
    public float fragmentDepth = 0.2f;

    [Header("Impacto")]
    public float impactRadiusMultiplier = 0.85f;
    public float normalForceWeight = 0.75f;
    public float radialForceWeight = 1.15f;
    public float randomForceWeight = 0.15f;
    public float nearSizeMultiplier = 0.55f;
    public float farSizeMultiplier = 1.2f;

    public void Shatter(Vector3 impactPoint, Vector3 impactNormal, float impactForce)
    {
        Mesh sourceMesh = GetComponent<MeshFilter>().mesh;
        Material mat = fragmentMaterial != null
            ? fragmentMaterial
            : GetComponent<MeshRenderer>().material;

        Bounds bounds = sourceMesh.bounds;
        Vector3 inheritedVelocity = Vector3.zero;

        // NUEVO: Buscamos la velocidad en el Padre (EnemyCore)
        EnemyCore tm = GetComponentInParent<EnemyCore>();
        if (tm != null)
        {
            inheritedVelocity = tm.MoveDirection * tm.speed;
        }

        Vector3 localImpactPoint = transform.InverseTransformPoint(impactPoint);
        Vector3 worldImpactNormal = impactNormal.normalized;

        int cutsX = Mathf.Max(1, Mathf.RoundToInt(Mathf.Sqrt(fragmentCount * (bounds.size.x / Mathf.Max(bounds.size.y, 0.001f)))));
        int cutsY = Mathf.Max(1, Mathf.RoundToInt((float)fragmentCount / cutsX));

        float[] xCuts = GenerateRandomCuts(bounds.min.x, bounds.max.x, cutsX);
        float[] yCuts = GenerateRandomCuts(bounds.min.y, bounds.max.y, cutsY);

        float max2DDistance = new Vector2(bounds.extents.x, bounds.extents.y).magnitude * impactRadiusMultiplier;
        max2DDistance = Mathf.Max(max2DDistance, 0.001f);

        int fragIndex = 0;

        for (int gx = 0; gx < xCuts.Length - 1; gx++)
        {
            for (int gy = 0; gy < yCuts.Length - 1; gy++)
            {
                float x0 = xCuts[gx];
                float x1 = xCuts[gx + 1];
                float y0 = yCuts[gy];
                float y1 = yCuts[gy + 1];

                float baseCellW = x1 - x0;
                float baseCellH = y1 - y0;

                Vector3 localCellCenter = new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, bounds.center.z);
                Vector3 worldCellCenter = transform.TransformPoint(localCellCenter);

                Vector2 impact2D = new Vector2(localImpactPoint.x, localImpactPoint.y);
                Vector2 cell2D = new Vector2(localCellCenter.x, localCellCenter.y);

                float distance = Vector2.Distance(cell2D, impact2D);
                float proximity = 1f - Mathf.Clamp01(distance / max2DDistance);

                float sizeFactor = Mathf.Lerp(farSizeMultiplier, nearSizeMultiplier, proximity);
                float shapedW = Mathf.Max(0.03f, baseCellW * sizeFactor);
                float shapedH = Mathf.Max(0.03f, baseCellH * sizeFactor);

                float localNoiseStrength = Mathf.Lerp(noiseStrength * 0.45f, noiseStrength * 1.35f, proximity);
                float localDepth = Mathf.Lerp(fragmentDepth * 0.9f, fragmentDepth * 1.15f, proximity);

                Mesh fragmentMesh = BuildFragmentMesh(shapedW, shapedH, localDepth, localNoiseStrength, fragIndex);

                SpawnFragment(fragmentMesh, mat, impactPoint, worldImpactNormal, worldCellCenter, inheritedVelocity, impactForce, proximity);
                fragIndex++;
            }
        }

        // Escondemos la malla del cubo. 
        // El EnemyCore (Padre) se encargará de destruir el objeto entero un instante después.
        GetComponent<MeshRenderer>().enabled = false;
    }

    private float[] GenerateRandomCuts(float min, float max, int count)
    {
        float[] cuts = new float[count + 1];
        cuts[0] = min;
        cuts[count] = max;

        List<float> interior = new List<float>();
        for (int i = 0; i < count - 1; i++) interior.Add(Random.Range(min + (max - min) * 0.1f, max - (max - min) * 0.1f));
        interior.Sort();
        for (int i = 0; i < interior.Count; i++) cuts[i + 1] = interior[i];

        return cuts;
    }

    private Mesh BuildFragmentMesh(float w, float h, float depth, float localNoiseStrength, int fragIndex)
    {
        int subDiv = 3;
        int vertsPerSide = subDiv + 1;
        int faceVerts = vertsPerSide * vertsPerSide;

        Vector3[] frontVerts = new Vector3[faceVerts];
        Vector2[] frontUVs = new Vector2[faceVerts];

        float seedX = fragIndex * 17.3f;
        float seedY = fragIndex * 13.7f;
        float halfDepth = depth * 0.5f;

        for (int y = 0; y < vertsPerSide; y++)
        {
            for (int x = 0; x < vertsPerSide; x++)
            {
                float tx = (float)x / subDiv;
                float ty = (float)y / subDiv;

                float bx = (tx - 0.5f) * w;
                float by = (ty - 0.5f) * h;

                float noiseDisplaceX = 0f;
                float noiseDisplaceY = 0f;

                if (x > 0 && x < subDiv && y > 0 && y < subDiv)
                {
                    float n1 = Mathf.PerlinNoise(seedX + tx * noiseScale, seedY + ty * noiseScale);
                    float n2 = Mathf.PerlinNoise(seedX + ty * noiseScale + 5f, seedY + tx * noiseScale + 5f);
                    noiseDisplaceX = (n1 - 0.5f) * 2f * localNoiseStrength * w;
                    noiseDisplaceY = (n2 - 0.5f) * 2f * localNoiseStrength * h;
                }

                frontVerts[y * vertsPerSide + x] = new Vector3(bx + noiseDisplaceX, by + noiseDisplaceY, -halfDepth);
                frontUVs[y * vertsPerSide + x] = new Vector2(tx, ty);
            }
        }

        Vector3[] backVerts = new Vector3[faceVerts];
        Vector2[] backUVs = new Vector2[faceVerts];

        for (int i = 0; i < faceVerts; i++)
        {
            backVerts[i] = new Vector3(frontVerts[i].x, frontVerts[i].y, halfDepth);
            backUVs[i] = frontUVs[i];
        }

        List<Vector2Int> perimeterIndices = new List<Vector2Int>();
        for (int x = 0; x < subDiv; x++) perimeterIndices.Add(new Vector2Int(x, 0));
        for (int y = 0; y < subDiv; y++) perimeterIndices.Add(new Vector2Int(subDiv, y));
        for (int x = subDiv; x > 0; x--) perimeterIndices.Add(new Vector2Int(x, subDiv));
        for (int y = subDiv; y > 0; y--) perimeterIndices.Add(new Vector2Int(0, y));

        int perimCount = perimeterIndices.Count;
        int sideVertCount = perimCount * 4;

        Vector3[] sideVerts = new Vector3[sideVertCount];
        Vector2[] sideUVs = new Vector2[sideVertCount];

        for (int i = 0; i < perimCount; i++)
        {
            int next = (i + 1) % perimCount;
            Vector2Int idxA = perimeterIndices[i];
            Vector2Int idxB = perimeterIndices[next];

            Vector3 a = frontVerts[idxA.y * vertsPerSide + idxA.x];
            Vector3 b = frontVerts[idxB.y * vertsPerSide + idxB.x];
            Vector3 c = backVerts[idxB.y * vertsPerSide + idxB.x];
            Vector3 d = backVerts[idxA.y * vertsPerSide + idxA.x];

            int vi = i * 4;
            sideVerts[vi] = a;
            sideVerts[vi + 1] = b;
            sideVerts[vi + 2] = c;
            sideVerts[vi + 3] = d;

            float u0 = (float)i / perimCount;
            float u1 = (float)(i + 1) / perimCount;

            sideUVs[vi] = new Vector2(u0, 0);
            sideUVs[vi + 1] = new Vector2(u1, 0);
            sideUVs[vi + 2] = new Vector2(u1, 1);
            sideUVs[vi + 3] = new Vector2(u0, 1);
        }

        int totalVerts = faceVerts * 2 + sideVertCount;
        Vector3[] allVerts = new Vector3[totalVerts];
        Vector2[] allUVs = new Vector2[totalVerts];

        System.Array.Copy(frontVerts, 0, allVerts, 0, faceVerts);
        System.Array.Copy(backVerts, 0, allVerts, faceVerts, faceVerts);
        System.Array.Copy(sideVerts, 0, allVerts, faceVerts * 2, sideVertCount);

        System.Array.Copy(frontUVs, 0, allUVs, 0, faceVerts);
        System.Array.Copy(backUVs, 0, allUVs, faceVerts, faceVerts);
        System.Array.Copy(sideUVs, 0, allUVs, faceVerts * 2, sideVertCount);

        List<int> tris = new List<int>();

        for (int y = 0; y < subDiv; y++)
        {
            for (int x = 0; x < subDiv; x++)
            {
                int bl = y * vertsPerSide + x;
                int br = bl + 1;
                int tl = bl + vertsPerSide;
                int tr = tl + 1;

                tris.Add(bl); tris.Add(tl); tris.Add(tr);
                tris.Add(bl); tris.Add(tr); tris.Add(br);
            }
        }

        int backOffset = faceVerts;
        for (int y = 0; y < subDiv; y++)
        {
            for (int x = 0; x < subDiv; x++)
            {
                int bl = backOffset + y * vertsPerSide + x;
                int br = bl + 1;
                int tl = bl + vertsPerSide;
                int tr = tl + 1;

                tris.Add(bl); tris.Add(tr); tris.Add(tl);
                tris.Add(bl); tris.Add(br); tris.Add(tr);
            }
        }

        int sideOffset = faceVerts * 2;
        for (int i = 0; i < perimCount; i++)
        {
            int vi = sideOffset + i * 4;
            tris.Add(vi); tris.Add(vi + 1); tris.Add(vi + 2);
            tris.Add(vi); tris.Add(vi + 2); tris.Add(vi + 3);
        }

        Mesh mesh = new Mesh();
        mesh.name = $"Fragment_{fragIndex}";
        mesh.vertices = allVerts;
        mesh.uv = allUVs;
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void SpawnFragment(Mesh mesh, Material mat, Vector3 impactPoint, Vector3 impactNormal, Vector3 worldCellCenter, Vector3 inheritedVelocity, float impactForce, float proximity)
    {
        GameObject frag = new GameObject("Frag");
        frag.transform.position = worldCellCenter;
        frag.transform.rotation = transform.rotation;
        frag.transform.localScale = Vector3.one;

        frag.AddComponent<MeshFilter>().mesh = mesh;
        frag.AddComponent<MeshRenderer>().material = mat;

        Rigidbody rb = frag.AddComponent<Rigidbody>();
        rb.useGravity = true;
        rb.linearVelocity = inheritedVelocity * 0.5f;

        Vector3 fromImpact = (worldCellCenter - impactPoint);
        Vector3 radialDir = fromImpact.sqrMagnitude > 0.0001f ? fromImpact.normalized : impactNormal;
        Vector3 randomDir = Random.onUnitSphere * randomForceWeight;

        Vector3 finalDir = radialDir * radialForceWeight + impactNormal * normalForceWeight + randomDir;
        finalDir.Normalize();

        float distance = Vector3.Distance(worldCellCenter, impactPoint);
        float falloff = 1f / (1f + distance * 1.75f);

        float finalForce = explosionForce * impactForce * Mathf.Lerp(0.55f, 1.8f, proximity) * falloff;

        rb.AddForce(finalDir * finalForce, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * explosionTorque * Mathf.Lerp(0.8f, 1.6f, proximity), ForceMode.Impulse);

        ShatterFragment sf = frag.AddComponent<ShatterFragment>();
        sf.lifetime = fragmentLifetime * Mathf.Lerp(1.15f, 0.85f, proximity) + Random.Range(-0.15f, 0.2f);
        sf.shrinkDelay = shrinkDelay;
    }
}