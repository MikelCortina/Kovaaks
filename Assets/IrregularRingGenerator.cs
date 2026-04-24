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

    [Header("Color")]
    public Gradient tunnelColors; // Definirá el color según la profundidad del anillo

    private Mesh mesh;
    private List<RingSnapshot> activeRings = new List<RingSnapshot>();
    private float currentInternalZ = 0f;
    private float tunnelOffset = 0f;


    [Header("Giro Continuo (Controlado por SO)")]
    public float turnStrengthX = 0f;
    public float turnStrengthY = 0f;
    public float curveStartDistance = 0f;
    void Start()
    {
        mesh = new Mesh();
        mesh.name = "Hybrid Mutability Tunnel";
        GetComponent<MeshFilter>().mesh = mesh;

        // Si no hay colores asignados, creamos uno blanco por defecto
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
        tunnelOffset -= speed * Time.deltaTime;

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

        for (int s = 0; s <= segments; s++)
        {
            float angle = (s / (float)segments) * Mathf.PI * 2f;
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

        activeRings.Add(newRing);
        UpdateRingData(activeRings.Count - 1);

        currentInternalZ += ringSpacing;
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

        for (int r = 0; r < ringCount; r++)
        {
            float depthPercent = (float)r / (ringCount - 1);
            Color ringColor = tunnelColors.Evaluate(depthPercent);

            // 1. Curva de serpiente (Ondas)
            float absoluteZ = activeRings[r].internalZForNoise;
            float waveOffsetX = Mathf.Sin(absoluteZ * curveFrequency) * curveX;
            float waveOffsetY = Mathf.Cos(absoluteZ * curveFrequency) * curveY;

            // 2. NUEVO: Giro continuo (Parábola)
            // Calculamos cuánta distancia ha pasado desde el "punto de inicio" de la curva
            float distanceAhead = Mathf.Max(0, currentZ - curveStartDistance);

            // Elevamos al cuadrado la distancia para lograr un giro suave pero constante (multiplicado por 0.001 para suavizar valores)
            float bendFactor = (distanceAhead * distanceAhead) * 0.001f;

            float continuousOffsetX = turnStrengthX * bendFactor;
            float continuousOffsetY = turnStrengthY * bendFactor;

            // Sumamos ambas curvas
            float finalOffsetX = waveOffsetX + continuousOffsetX;
            float finalOffsetY = waveOffsetY + continuousOffsetY;

            for (int s = 0; s <= segments; s++)
            {
                int index = r * (segments + 1) + s;
                Vector3 localPos = activeRings[r].localVertices[s];

                // Aplicamos los offsets finales
                vertices[index] = new Vector3(localPos.x + finalOffsetX, localPos.y + finalOffsetY, currentZ);
                uvs[index] = new Vector2((float)s / segments, (float)r / ringCount);
                colors[index] = ringColor;
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
            flatColors[i] = colors[oldIndex]; // Aplanamos el array de colores también
            flatTriangles[i] = i;
        }

        mesh.Clear();
        mesh.vertices = flatVertices;
        mesh.uv = flatUvs;
        mesh.colors = flatColors; // Asignamos los colores al mesh
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

        if (worldZ <= firstRingWorldZ) return GetRingOffset(0, firstRingWorldZ);

        float currentZ = firstRingWorldZ;
        for (int i = 0; i < activeRings.Count - 1; i++)
        {
            float nextZ = currentZ + activeRings[i].spacingToNext;

            if (worldZ >= currentZ && worldZ <= nextZ)
            {
                float percent = (worldZ - currentZ) / (nextZ - currentZ);
                Vector2 offsetA = GetRingOffset(i, currentZ);
                Vector2 offsetB = GetRingOffset(i + 1, nextZ);

                return Vector2.Lerp(offsetA, offsetB, percent);
            }
            currentZ = nextZ;
        }

        // CORRECCIÓN: Usamos worldZ en lugar de currentZ. 
        // Así, si el spawner está más lejos que el final del túnel, la curva parabólica se sigue calculando en el vacío.
        return GetRingOffset(activeRings.Count - 1, worldZ);
    }

    private Vector2 GetRingOffset(int ringIndex, float ringWorldZ)
    {
        float absoluteZ = activeRings[ringIndex].internalZForNoise;

        float waveOffsetX = Mathf.Sin(absoluteZ * curveFrequency) * curveX;
        float waveOffsetY = Mathf.Cos(absoluteZ * curveFrequency) * curveY;

        float distanceAhead = Mathf.Max(0, ringWorldZ - curveStartDistance);
        float bendFactor = (distanceAhead * distanceAhead) * 0.001f;
        float continuousOffsetX = turnStrengthX * bendFactor;
        float continuousOffsetY = turnStrengthY * bendFactor;

        return new Vector2(waveOffsetX + continuousOffsetX, waveOffsetY + continuousOffsetY);
    }
}