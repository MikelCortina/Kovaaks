using System.Collections; // Necesario para las Corrutinas
using System.Collections.Generic;
using UnityEngine;

public class TargetSpawner : MonoBehaviour
{
    public enum ShapeType { Rectangular, Circular }

    [Header("Configuración General")]
    public GameObject targetPrefab;
    public Transform playerTransform;
    public float spawnInterval = 2f;
    [Tooltip("Tiempo en segundos para que aparezca un objeto extra a la vez")]
    public float difficultyIncreaseInterval = 10f;

    [Tooltip("Tiempo que tarda el objeto en crecer desde escala 0")]
    public float scaleDuration = 0.5f; // NUEVO: Duración de la animación de escala

    [Header("Configuración de Forma")]
    public ShapeType shape = ShapeType.Rectangular;
    public float gridSpacing = 2f;
    public int rows = 3;
    public int columns = 3;
    public float circleRadius = 5f;
    public int circlePoints = 8;

    private float spawnTimer;
    private float difficultyTimer;
    private int targetsToSpawn = 1; // Empezamos spawneando solo 1

    void Update()
    {
        // Aumentar la cantidad de objetos cada 10 segundos
        difficultyTimer += Time.deltaTime;
        if (difficultyTimer >= difficultyIncreaseInterval)
        {
            targetsToSpawn++;
            difficultyTimer = 0f;
        }

        // Spawneo regular
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            SpawnTargets();
            spawnTimer = 0f;
        }
    }

    void SpawnTargets()
    {
        // 1. Obtenemos todas las celdas posibles
        List<Vector3> allSpawnPoints = GetAllSpawnPoints();

        // 2. Limitamos para no spawnear más de lo que la cuadrícula permite
        int spawnCount = Mathf.Min(targetsToSpawn, allSpawnPoints.Count);

        // 3. Barajamos la lista para elegir celdas aleatorias
        ShuffleList(allSpawnPoints);

        // 4. EL TRUCO: Calculamos la dirección desde el CENTRO del spawner, no desde el objeto.
        Vector3 parallelDirection = (playerTransform.position - transform.position).normalized;

        // 5. Instanciamos solo la cantidad de objetos que toca
        for (int i = 0; i < spawnCount; i++)
        {
            InstantiateTarget(allSpawnPoints[i], parallelDirection);
        }
    }

    List<Vector3> GetAllSpawnPoints()
    {
        List<Vector3> points = new List<Vector3>();

        if (shape == ShapeType.Rectangular)
        {
            Vector3 startPos = transform.position - new Vector3((columns - 1) * gridSpacing / 2f, (rows - 1) * gridSpacing / 2f, 0);
            for (int x = 0; x < columns; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    points.Add(startPos + new Vector3(x * gridSpacing, y * gridSpacing, 0));
                }
            }
        }
        else if (shape == ShapeType.Circular)
        {
            float angleStep = 360f / circlePoints;
            for (int i = 0; i < circlePoints; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                points.Add(transform.position + new Vector3(Mathf.Cos(angle) * circleRadius, Mathf.Sin(angle) * circleRadius, 0));
            }
        }

        return points;
    }

    void InstantiateTarget(Vector3 position, Vector3 direction)
    {
        GameObject newTarget = Instantiate(targetPrefab, position, Quaternion.identity);
        TargetMovement movementScript = newTarget.GetComponent<TargetMovement>();

        if (movementScript != null)
        {
            movementScript.SetDirection(direction);
        }

        // NUEVO: Iniciar la corrutina para escalar el objeto
        StartCoroutine(ScaleUpAnimation(newTarget.transform));
    }

    // NUEVO: Corrutina que maneja el crecimiento
    IEnumerator ScaleUpAnimation(Transform targetTransform)
    {
        // Guardamos la escala original que tiene el prefab
        Vector3 finalScale = targetPrefab.transform.localScale;

        // Empezamos con escala 0
        targetTransform.localScale = Vector3.zero;

        float timer = 0f;

        while (timer < scaleDuration)
        {
            // Comprobamos si el objeto fue destruido antes de terminar de crecer
            if (targetTransform == null) yield break;

            timer += Time.deltaTime;

            // Calculamos el progreso (de 0 a 1)
            float percent = timer / scaleDuration;

            // Interpolamos desde Vector3.zero hasta la escala final
            targetTransform.localScale = Vector3.Lerp(Vector3.zero, finalScale, percent);

            // Esperamos al siguiente frame
            yield return null;
        }

        // Aseguramos que termine exactamente en la escala final
        if (targetTransform != null)
        {
            targetTransform.localScale = finalScale;
        }
    }

    void ShuffleList(List<Vector3> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            Vector3 temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        List<Vector3> points = GetAllSpawnPoints();
        foreach (Vector3 p in points)
        {
            Gizmos.DrawWireSphere(p, 0.3f);
        }
    }
}