using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetSpawner : MonoBehaviour
{
    public enum ShapeType { Rectangular, Circular }

    [Header("Referencia")]
    public GameObject targetPrefab;
    public Transform playerTransform;
    public InfiniteProceduralTunnel tunnel;

    [Header("Spawn Continuo")]
    public float initialSpawnInterval = 1.5f;
    public float minSpawnInterval = 0.2f;
    public float difficultyRampTime = 30f;
    public AnimationCurve difficultyCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Animación")]
    public float scaleDuration = 0.5f;

    [Header("Forma")]
    public ShapeType shape = ShapeType.Rectangular;
    public float gridSpacing = 2f;
    public int rows = 3;
    public int columns = 3;
    public float circleRadius = 5f;
    public int circlePoints = 8;

    private float spawnTimer;
    private float gameTime;

    // NUEVO: Guardamos dónde debería estar el spawner si el túnel fuera 100% recto
    private Vector3 logicalPosition;

    void Start()
    {
        // Guardamos su posición inicial recta
        logicalPosition = transform.position;
    }

    void Update()
    {
        // 1. EL SPAWNER AHORA NAVEGA LA CURVA: Lo movemos físicamente para que siga dentro del túnel
        if (tunnel != null)
        {
            Vector2 spawnerOffset = tunnel.GetCurveOffset(logicalPosition.z);
            Vector3 curvedPos = new Vector3(logicalPosition.x + spawnerOffset.x, logicalPosition.y + spawnerOffset.y, logicalPosition.z);
            transform.position = curvedPos;

            // Hacemos que el Spawner también mire hacia donde gira la curva
            float lookAheadZ = logicalPosition.z - 1f;
            Vector2 offsetAhead = tunnel.GetCurveOffset(lookAheadZ);
            Vector3 forwardPos = new Vector3(logicalPosition.x + offsetAhead.x, logicalPosition.y + offsetAhead.y, lookAheadZ);
            Vector3 pathDirection = (forwardPos - curvedPos).normalized;

            if (pathDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(pathDirection);
            }
        }

        // 2. Lógica normal de tiempo
        gameTime += Time.deltaTime;
        float difficultyPercent = Mathf.Clamp01(gameTime / difficultyRampTime);
        float curveValue = difficultyCurve.Evaluate(difficultyPercent);
        float currentInterval = Mathf.Lerp(initialSpawnInterval, minSpawnInterval, curveValue);

        spawnTimer += Time.deltaTime;

        if (spawnTimer >= currentInterval)
        {
            SpawnOne();
            spawnTimer = 0f;
        }
    }

    void SpawnOne()
    {
        // Usamos la posición lógica para no aplicar la curva 2 veces
        List<Vector3> points = GetLogicalSpawnPoints(logicalPosition);
        if (points.Count == 0) return;

        Vector3 spawnPos = points[Random.Range(0, points.Count)];
        Vector3 direction = Vector3.back;

        InstantiateTarget(spawnPos, direction);
    }

    // NUEVO: Función auxiliar para calcular la cuadrícula basándose en un centro "recto"
    List<Vector3> GetLogicalSpawnPoints(Vector3 center)
    {
        List<Vector3> points = new List<Vector3>();

        if (shape == ShapeType.Rectangular)
        {
            Vector3 startPos = center - new Vector3((columns - 1) * gridSpacing / 2f, (rows - 1) * gridSpacing / 2f, 0);

            for (int x = 0; x < columns; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    points.Add(startPos + new Vector3(x * gridSpacing, y * gridSpacing, 0));
                }
            }
        }
        else
        {
            float angleStep = 360f / circlePoints;

            for (int i = 0; i < circlePoints; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                points.Add(center + new Vector3(Mathf.Cos(angle) * circleRadius, Mathf.Sin(angle) * circleRadius, 0));
            }
        }
        return points;
    }

    void InstantiateTarget(Vector3 logicalSpawnPos, Vector3 direction)
    {
        GameObject newTarget = Instantiate(targetPrefab, logicalSpawnPos, Quaternion.identity);
        TargetMovement movement = newTarget.GetComponent<TargetMovement>();

        if (movement != null)
        {
            // Le pasamos la posición recta al cubo, y él ya se encarga de saltar a la curva en su Initialize
            movement.Initialize(logicalSpawnPos, direction, tunnel);
        }

        StartCoroutine(ScaleUpAnimation(newTarget.transform));
    }

    IEnumerator ScaleUpAnimation(Transform t)
    {
        Vector3 finalScale = targetPrefab.transform.localScale;
        t.localScale = Vector3.zero;
        float timer = 0f;

        while (timer < scaleDuration)
        {
            if (t == null) yield break;
            timer += Time.deltaTime;
            float percent = timer / scaleDuration;
            t.localScale = Vector3.Lerp(Vector3.zero, finalScale, percent);
            yield return null;
        }

        if (t != null) t.localScale = finalScale;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;

        // Si estamos en Play, usamos la lógica. Si no, la posición del editor.
        Vector3 basePos = Application.isPlaying ? logicalPosition : transform.position;
        List<Vector3> logicPoints = GetLogicalSpawnPoints(basePos);

        foreach (var p in logicPoints)
        {
            Vector3 drawPos = p;
            if (tunnel != null)
            {
                Vector2 offset = tunnel.GetCurveOffset(p.z);
                drawPos = new Vector3(p.x + offset.x, p.y + offset.y, p.z);
            }
            Gizmos.DrawWireSphere(drawPos, 0.3f);
        }
    }
}