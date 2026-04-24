using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class InfiniteProceduralTunnel : MonoBehaviour
{
    private struct RingSnapshot
    {
        public Vector3[] localVertices;
        public float spacingToNext;
        public float internalZForNoise;
        public int ringIndex;
    }

    [Header("Estructura del Túnel")]
    public int segments = 24;
    public int ringCount = 40;
    public float ringSpacing = 1f;
    public float baseRadius = 5f;
    public int ringsBehindPlayer = 5;

    [Header("Control de Mutabilidad")]
    public float changeDistance = 80f;

    [Header("Irregularidad")]
    public float noiseScale = 1.5f;
    public float noiseZScale = 0.1f;
    public float noiseStrength = 2f;

    [Header("Movimiento y Curvas")]
    public float speed = 10f;
    public float curveX = 0f;
    public float curveY = 0f;
    public float curveFrequency = 0.05f;

    [Header("Giro Continuo (Controlado por SO)")]
    public float turnStrengthX = 0f;
    public float turnStrengthY = 0f;
    public float curveStartDistance = 0f;

    [Header("Torsión (Controlado por SO)")]
    [HideInInspector] public float twistDegreesPerMeter = 0f;

    [Header("Rotación Global (Controlado por SO)")]
    [HideInInspector] public float globalRotationSpeed = 0f;
    private float currentGlobalRotation = 0f;

    [Header("Respiración de Paredes (Controlado por SO)")]
    [HideInInspector] public float wobbleSpeed = 0f;
    [HideInInspector] public float wobbleStrength = 0f;
    [HideInInspector] public float wobbleNoiseScale = 0f; // NUEVO: Escala del caos
    private float currentWobbleTime = 0f;

    [Header("Color")]
    public Gradient tunnelColors;

    [HideInInspector] public float colorCycleSpeed = 0f;
    private float colorOffset = 0f;

    [Header("Highlight (Controlado por SO)")]
    [HideInInspector] public bool useHighlight = false;
    [HideInInspector] public HighlightTunnelEffectSO.HighlightMode highlightMode;
    [HideInInspector] public Color highlightColor = Color.white;
    [HideInInspector] public int highlightStep = 0;

    private Mesh mesh;
    private List<RingSnapshot> activeRings = new List<RingSnapshot>();
    private float currentInternalZ = 0f;
    private float tunnelOffset = 0f;

    private int globalRingCounter = 0;

    void Start()
    {
        mesh = new Mesh();
        mesh.name = "Hybrid Mutability Tunnel";
        GetComponent<MeshFilter>().mesh = mesh;

        if (tunnelColors == null)
        {
            tunnelColors = new Gradient();
            tunnelColors.SetKeys(new GradientColorKey[] { new GradientColorKey(Color.white, 0f) }, new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f) });
        }

        for (int i = 0; i < ringCount; i++)
        {
            CreateNewRingSnapshot();
        }
    }

    void Update()
    {
        colorOffset += colorCycleSpeed * Time.deltaTime;
        tunnelOffset -= speed * Time.deltaTime;
        currentGlobalRotation += globalRotationSpeed * Time.deltaTime;

        currentWobbleTime += wobbleSpeed * Time.deltaTime;

        if (tunnelOffset <= -activeRings[0].spacingToNext)
        {
            tunnelOffset += activeRings[0].spacingToNext;
            activeRings.RemoveAt(0);
            CreateNewRingSnapshot();
        }

        MutateDistantRings();
        UpdateMeshGeometry();
    }

    void MutateDistantRings()
    {
        float startOffsetZ = 0;
        for (int i = 0; i < ringsBehindPlayer; i++) startOffsetZ -= activeRings[i].spacingToNext;
        float currentZ = startOffsetZ + tunnelOffset;

        for (int i = 0; i < activeRings.Count; i++)
        {
            if (currentZ > changeDistance)
            {
                UpdateRingData(i);
            }
            currentZ += activeRings[i].spacingToNext;
        }
    }

    void UpdateRingData(int index)
    {
        RingSnapshot ring = activeRings[index];
        ring.spacingToNext = ringSpacing;

        float twistRotationRadians = (ring.internalZForNoise * twistDegreesPerMeter) * Mathf.Deg2Rad;

        for (int s = 0; s <= segments; s++)
        {
            float angle = ((s / (float)segments) * Mathf.PI * 2f) + twistRotationRadians;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            float n1 = Mathf.PerlinNoise(cos * noiseScale + 100f, ring.internalZForNoise * noiseZScale);
            float n2 = Mathf.PerlinNoise(sin * noiseScale + 100f, ring.internalZForNoise * noiseZScale);
            float noise = (n1 + n2) * 0.5f - 0.5f;

            float r = baseRadius + (noise * noiseStrength);
            ring.localVertices[s] = new Vector3(cos * r, sin * r, 0);
        }
        activeRings[index] = ring;
    }

    void CreateNewRingSnapshot()
    {
        RingSnapshot newRing = new RingSnapshot();
        newRing.localVertices = new Vector3[segments + 1];
        newRing.internalZForNoise = currentInternalZ;
        newRing.spacingToNext = ringSpacing;
        newRing.ringIndex = globalRingCounter;

        activeRings.Add(newRing);
        UpdateRingData(activeRings.Count - 1);

        currentInternalZ += ringSpacing;
        globalRingCounter++;
    }

    void UpdateMeshGeometry()
    {
        int numVertices = ringCount * (segments + 1);
        Vector3[] vertices = new Vector3[numVertices];
        Vector2[] uvs = new Vector2[numVertices];
        Color[] colors = new Color[numVertices];

        float startOffsetZ = 0;
        for (int i = 0; i < ringsBehindPlayer; i++) startOffsetZ -= activeRings[i].spacingToNext;
        float currentZ = startOffsetZ + tunnelOffset;

        float globalRad = currentGlobalRotation * Mathf.Deg2Rad;
        float cosGlobal = Mathf.Cos(globalRad);
        float sinGlobal = Mathf.Sin(globalRad);

        for (int r = 0; r < ringCount; r++)
        {
            float baseDepth = (float)r / (ringCount - 1);
            float depthPercent = Mathf.Repeat(baseDepth + colorOffset, 1f);
            Color ringColor = tunnelColors.Evaluate(depthPercent);

            float absoluteZ = activeRings[r].internalZForNoise;
            float waveOffsetX = Mathf.Sin(absoluteZ * curveFrequency) * curveX;
            float waveOffsetY = Mathf.Cos(absoluteZ * curveFrequency) * curveY;

            float distanceAhead = Mathf.Max(0, currentZ - curveStartDistance);
            float bendFactor = (distanceAhead * distanceAhead) * 0.001f;
            float continuousOffsetX = turnStrengthX * bendFactor;
            float continuousOffsetY = turnStrengthY * bendFactor;

            float finalOffsetX = waveOffsetX + continuousOffsetX;
            float finalOffsetY = waveOffsetY + continuousOffsetY;

            for (int s = 0; s <= segments; s++)
            {
                int index = r * (segments + 1) + s;
                Vector3 localPos = activeRings[r].localVertices[s];

                // NUEVO: Respiración aleatoria (Wobble Caótico con Perlin Noise)
                if (wobbleStrength > 0f)
                {
                    // Usamos las coordenadas normalizadas del círculo (Seno/Coseno) y la Z para generar un punto en un espacio 3D
                    float angle = (s / (float)segments) * Mathf.PI * 2f;
                    float circleX = Mathf.Cos(angle);
                    float circleY = Mathf.Sin(angle);

                    // Generamos ruido Perlin que fluye con el tiempo
                    float noiseX = Mathf.PerlinNoise(circleX * wobbleNoiseScale + currentWobbleTime, absoluteZ * 0.1f * wobbleNoiseScale);
                    float noiseY = Mathf.PerlinNoise(circleY * wobbleNoiseScale - currentWobbleTime, absoluteZ * 0.1f * wobbleNoiseScale);

                    // Centramos el ruido (-1 a 1)
                    float chaoticWobble = ((noiseX + noiseY) - 1f);

                    Vector3 directionFromCenter = localPos.normalized;
                    localPos += directionFromCenter * (chaoticWobble * wobbleStrength);
                }

                float xBase = localPos.x + finalOffsetX;
                float yBase = localPos.y + finalOffsetY;

                float xRotated = xBase * cosGlobal - yBase * sinGlobal;
                float yRotated = xBase * sinGlobal + yBase * cosGlobal;

                vertices[index] = new Vector3(xRotated, yRotated, currentZ);
                uvs[index] = new Vector2((float)s / segments, (float)r / ringCount);

                Color finalVertexColor = ringColor;
                if (useHighlight && highlightStep > 0)
                {
                    if (highlightMode == HighlightTunnelEffectSO.HighlightMode.Longitudinal)
                    {
                        if (s % highlightStep == 0) finalVertexColor = highlightColor;
                    }
                    else if (highlightMode == HighlightTunnelEffectSO.HighlightMode.Transversal)
                    {
                        if (activeRings[r].ringIndex % highlightStep == 0) finalVertexColor = highlightColor;
                    }
                }
                colors[index] = finalVertexColor;
            }
            currentZ += activeRings[r].spacingToNext;
        }

        ApplyFlatMesh(vertices, uvs, colors);
    }

    void ApplyFlatMesh(Vector3[] vertices, Vector2[] uvs, Color[] colors)
    {
        int[] baseTriangles = GenerateTrianglesIndices();
        Vector3[] flatVertices = new Vector3[baseTriangles.Length];
        Vector2[] flatUvs = new Vector2[baseTriangles.Length];
        Color[] flatColors = new Color[baseTriangles.Length];
        int[] flatTriangles = new int[baseTriangles.Length];

        for (int i = 0; i < baseTriangles.Length; i++)
        {
            int oldIndex = baseTriangles[i];
            flatVertices[i] = vertices[oldIndex];
            flatUvs[i] = uvs[oldIndex];
            flatColors[i] = colors[oldIndex];
            flatTriangles[i] = i;
        }

        mesh.Clear();
        mesh.vertices = flatVertices;
        mesh.uv = flatUvs;
        mesh.colors = flatColors;
        mesh.triangles = flatTriangles;
        mesh.RecalculateNormals();
    }

    int[] GenerateTrianglesIndices()
    {
        int[] tris = new int[(ringCount - 1) * segments * 6];
        int t = 0;
        for (int ring = 0; ring < ringCount - 1; ring++)
        {
            for (int seg = 0; seg < segments; seg++)
            {
                int current = ring * (segments + 1) + seg;
                int nextRing = current + segments + 1;
                tris[t++] = current; tris[t++] = nextRing; tris[t++] = nextRing + 1;
                tris[t++] = current; tris[t++] = nextRing + 1; tris[t++] = current + 1;
            }
        }
        return tris;
    }

    public Vector2 GetCurveOffset(float worldZ)
    {
        if (activeRings.Count == 0) return Vector2.zero;

        float startOffsetZ = 0;
        for (int i = 0; i < ringsBehindPlayer; i++) startOffsetZ -= activeRings[i].spacingToNext;
        float firstRingWorldZ = startOffsetZ + tunnelOffset;

        if (worldZ <= firstRingWorldZ) return GetRingOffsetRotated(0, firstRingWorldZ);

        float currentZ = firstRingWorldZ;
        for (int i = 0; i < activeRings.Count - 1; i++)
        {
            float nextZ = currentZ + activeRings[i].spacingToNext;

            if (worldZ >= currentZ && worldZ <= nextZ)
            {
                float percent = (worldZ - currentZ) / (nextZ - currentZ);
                Vector2 offsetA = GetRingOffsetRotated(i, currentZ);
                Vector2 offsetB = GetRingOffsetRotated(i + 1, nextZ);

                return Vector2.Lerp(offsetA, offsetB, percent);
            }
            currentZ = nextZ;
        }

        return GetRingOffsetRotated(activeRings.Count - 1, worldZ);
    }

    private Vector2 GetRingOffsetRotated(int ringIndex, float ringWorldZ)
    {
        float absoluteZ = activeRings[ringIndex].internalZForNoise;

        float waveOffsetX = Mathf.Sin(absoluteZ * curveFrequency) * curveX;
        float waveOffsetY = Mathf.Cos(absoluteZ * curveFrequency) * curveY;

        float distanceAhead = Mathf.Max(0, ringWorldZ - curveStartDistance);
        float bendFactor = (distanceAhead * distanceAhead) * 0.001f;
        float continuousOffsetX = turnStrengthX * bendFactor;
        float continuousOffsetY = turnStrengthY * bendFactor;

        float xBase = waveOffsetX + continuousOffsetX;
        float yBase = waveOffsetY + continuousOffsetY;

        float globalRad = currentGlobalRotation * Mathf.Deg2Rad;
        float cosGlobal = Mathf.Cos(globalRad);
        float sinGlobal = Mathf.Sin(globalRad);

        float xRotated = xBase * cosGlobal - yBase * sinGlobal;
        float yRotated = xBase * sinGlobal + yBase * cosGlobal;

        return new Vector2(xRotated, yRotated);
    }
}