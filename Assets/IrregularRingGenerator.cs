using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class InfiniteProceduralTunnel : MonoBehaviour
{
    [Header("Estructura del Túnel")]
    public int segments = 24;          // Calidad del círculo (Vértices por anillo)
    public int ringCount = 40;         // Profundidad del túnel (Cuántos anillos lo forman)
    public float ringSpacing = 1f;     // Distancia entre cada anillo
    public float baseRadius = 5f;      // Tamaño del túnel
    public int ringsBehindPlayer = 5;

    [Header("Irregularidad (Generación de Cueva)")]
    public float noiseScale = 1.5f;    // Escala del ruido en los ejes X e Y
    public float noiseZScale = 0.1f;   // Escala del ruido a lo largo del túnel (eje Z)
    public float noiseStrength = 2f;   // Cuánto sobresalen las rocas

    [Header("Movimiento")]
    public float speed = 10f;          // Velocidad a la que avanzamos por el túnel

    private Mesh mesh;
    private Vector3[] vertices;
    private int[] triangles;
    private Vector2[] uvs;

    private float globalZOffset = 0f;  // Rastrea dónde estamos en el mapa de ruido "infinito"



    void Start()
    {
        // Configuramos la malla
        mesh = new Mesh();
        mesh.name = "Procedural Tunnel";
        GetComponent<MeshFilter>().mesh = mesh;

        // Inicializamos los arrays
        int numVertices = ringCount * (segments + 1);
        vertices = new Vector3[numVertices];
        uvs = new Vector2[numVertices];
        triangles = new int[(ringCount - 1) * segments * 6];

        // 1. PRIMERO calculamos las matemáticas de las conexiones (pero no las aplicamos aún)
        GenerateTriangles();

        // 2. LUEGO generamos los vértices, que automáticamente llamará al Flat Shading
        // y aplicará todo a la malla en el orden correcto sin dar errores.
        UpdateTunnelVertices();
    }

    void Update()
    {
        // 1. Movemos el túnel hacia la cámara (hacia atrás en Z)
        transform.Translate(0, 0, -speed * Time.deltaTime);

        // 2. Si el túnel ha retrocedido el equivalente a la distancia de un anillo...
        if (transform.position.z <= -ringSpacing)
        {
            // Lo teletransportamos hacia adelante para crear el bucle (efecto cinta de correr)
            transform.position += new Vector3(0, 0, ringSpacing);

            // Avanzamos nuestro rastreador de ruido para generar un nuevo trozo de cueva
            globalZOffset += ringSpacing;

            // Recalculamos la forma de la malla
            UpdateTunnelVertices();
        }
    }

    void UpdateTunnelVertices()
    {
        for (int r = 0; r < ringCount; r++)
        {
            // RESTAMOS los anillos para que la malla empiece detrás de nosotros
            float localZ = (r - ringsBehindPlayer) * ringSpacing;

            // Calculamos el ruido usando 'r' puro para que la forma sea consistente al reciclar
            float absoluteZ = globalZOffset + (r * ringSpacing);

            for (int s = 0; s <= segments; s++)
            {
                float angle = (s / (float)segments) * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                // TRUCO PARA RUIDO 3D EN UN CILINDRO:
                // Mezclamos dos ruidos 2D para simular un volumen orgánico sin "costuras"
                float noise1 = Mathf.PerlinNoise(cos * noiseScale + 100f, absoluteZ * noiseZScale);
                float noise2 = Mathf.PerlinNoise(sin * noiseScale + 100f, absoluteZ * noiseZScale);
                float noise = (noise1 + noise2) * 0.5f - 0.5f; // Rango de -0.5 a 0.5

                // Aplicamos la deformación
                float currentRadius = baseRadius + (noise * noiseStrength);

                // Calculamos la posición X e Y
                float x = cos * currentRadius;
                float y = sin * currentRadius;

                // Índice del vértice en el array
                int index = r * (segments + 1) + s;

                vertices[index] = new Vector3(x, y, localZ);

                // Mapeo básico de texturas (UVs)
                uvs[index] = new Vector2((float)s / segments, (float)r / ringCount);
            }
        }

        ApplyFlatShading();
    }

    void GenerateTriangles()
    {
        int t = 0;
        for (int ring = 0; ring < ringCount - 1; ring++)
        {
            for (int seg = 0; seg < segments; seg++)
            {
                // Buscamos los 4 vértices que forman un "cuadrado" entre dos anillos
                int current = ring * (segments + 1) + seg;
                int nextRing = current + segments + 1;

                // Primer triángulo
                triangles[t++] = current;
                triangles[t++] = nextRing;
                triangles[t++] = nextRing + 1;

                // Segundo triángulo
                triangles[t++] = current;
                triangles[t++] = nextRing + 1;
                triangles[t++] = current + 1;
            }
        }

        // ¡HEMOS BORRADO LA LÍNEA mesh.triangles = triangles; DE AQUÍ!
    }
    void ApplyFlatShading()
    {
        // Creamos nuevos arrays con el tamaño exacto de los triángulos
        Vector3[] flatVertices = new Vector3[triangles.Length];
        Vector2[] flatUvs = new Vector2[triangles.Length];
        int[] flatTriangles = new int[triangles.Length];

        // "Despegamos" los vértices para que cada triángulo tenga sus propios 3 puntos únicos
        for (int i = 0; i < triangles.Length; i++)
        {
            flatVertices[i] = vertices[triangles[i]];
            flatUvs[i] = uvs[triangles[i]];
            flatTriangles[i] = i; // Ahora los índices van de 1 en 1 (0, 1, 2, 3...)
        }

        // Asignamos la nueva geometría plana a la malla
        mesh.vertices = flatVertices;
        mesh.uv = flatUvs;
        mesh.triangles = flatTriangles;

        // Al recalcular ahora, Unity creará bordes 100% afilados
        mesh.RecalculateNormals();
    }
}