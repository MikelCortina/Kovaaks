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
        public float internalZForNoise; // Guardamos esto para poder regenerar el ruido correctamente
    }

    [Header("Estructura del Túnel")]
    public int segments = 24;
    public int ringCount = 40;
    public float ringSpacing = 1f;
    public float baseRadius = 5f;
    public int ringsBehindPlayer = 5;

    [Header("Control de Mutabilidad")]
    public float changeDistance = 80f; // A partir de aquí los anillos obedecen al Inspector

    [Header("Irregularidad")]
    public float noiseScale = 1.5f;
    public float noiseZScale = 0.1f;
    public float noiseStrength = 2f;

    [Header("Movimiento")]
    public float speed = 10f;

    private Mesh mesh;
    private List<RingSnapshot> activeRings = new List<RingSnapshot>();
    private float currentInternalZ = 0f;
    private float tunnelOffset = 0f;

    void Start()
    {
        mesh = new Mesh();
        mesh.name = "Hybrid Mutability Tunnel";
        GetComponent<MeshFilter>().mesh = mesh;

        for (int i = 0; i < ringCount; i++)
        {
            CreateNewRingSnapshot();
        }
    }

    void Update()
    {
        tunnelOffset -= speed * Time.deltaTime;

        // 1. Lógica de reciclaje (Spawn/Despawn)
        if (tunnelOffset <= -activeRings[0].spacingToNext)
        {
            tunnelOffset += activeRings[0].spacingToNext;
            activeRings.RemoveAt(0);
            CreateNewRingSnapshot();
        }

        // 2. Lógica de Mutación Dinámica por distancia
        MutateDistantRings();

        UpdateMeshGeometry();
    }

    void MutateDistantRings()
    {
        // Calculamos la posición Z actual de cada anillo para saber su distancia
        float startOffsetZ = 0;
        for (int i = 0; i < ringsBehindPlayer; i++) startOffsetZ -= activeRings[i].spacingToNext;
        float currentZ = startOffsetZ + tunnelOffset;

        for (int i = 0; i < activeRings.Count; i++)
        {
            // Si el anillo está más lejos de 'changeDistance', lo actualizamos
            if (currentZ > changeDistance)
            {
                UpdateRingData(i);
            }
            currentZ += activeRings[i].spacingToNext;
        }
    }

    // Esta función sobreescribe un anillo existente con los datos nuevos del Inspector
    void UpdateRingData(int index)
    {
        RingSnapshot ring = activeRings[index];
        ring.spacingToNext = ringSpacing; // Actualiza spacing

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
    UpdateRingData(activeRings.Count - 1); // Rellena tras añadir al final

    currentInternalZ += ringSpacing;
}

    void UpdateMeshGeometry()
    {
        int numVertices = ringCount * (segments + 1);
        Vector3[] vertices = new Vector3[numVertices];
        Vector2[] uvs = new Vector2[numVertices];

        float startOffsetZ = 0;
        for (int i = 0; i < ringsBehindPlayer; i++) startOffsetZ -= activeRings[i].spacingToNext;
        float currentZ = startOffsetZ + tunnelOffset;

        for (int r = 0; r < ringCount; r++)
        {
            for (int s = 0; s <= segments; s++)
            {
                int index = r * (segments + 1) + s;
                vertices[index] = new Vector3(activeRings[r].localVertices[s].x, activeRings[r].localVertices[s].y, currentZ);
                uvs[index] = new Vector2((float)s / segments, (float)r / ringCount);
            }
            currentZ += activeRings[r].spacingToNext;
        }

        ApplyFlatMesh(vertices, uvs);
    }

    void ApplyFlatMesh(Vector3[] vertices, Vector2[] uvs)
    {
        int[] baseTriangles = GenerateTrianglesIndices();
        Vector3[] flatVertices = new Vector3[baseTriangles.Length];
        Vector2[] flatUvs = new Vector2[baseTriangles.Length];
        int[] flatTriangles = new int[baseTriangles.Length];

        for (int i = 0; i < baseTriangles.Length; i++)
        {
            int oldIndex = baseTriangles[i];
            flatVertices[i] = vertices[oldIndex];
            flatUvs[i] = uvs[oldIndex];
            flatTriangles[i] = i;
        }

        mesh.Clear();
        mesh.vertices = flatVertices;
        mesh.uv = flatUvs;
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
}