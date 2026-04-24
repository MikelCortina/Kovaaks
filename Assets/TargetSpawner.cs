using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TargetSpawner : MonoBehaviour
{
    public enum ShapeType { Rectangular, Circular }

    [Header("Configuración del Nivel")]
    public LevelDataSO currentLevel;
    public bool autoStartLevel = true;
    public TunnelEffectController effectController;

    [Header("Referencia")]
    public GameObject targetPrefab;
    public Transform playerTransform;
    public InfiniteProceduralTunnel tunnel;

    [Header("Forma y Animación")]
    public float scaleDuration = 0.5f;
    public ShapeType shape = ShapeType.Rectangular;
    public float gridSpacing = 2f;
    public int rows = 3;
    public int columns = 3;
    public float circleRadius = 5f;
    public int circlePoints = 8;

    [Header("Eventos de UI")]
    [Tooltip("Se dispara al terminar el nivel")]
    public UnityEvent<float> OnLevelCompleted;

    [Tooltip("Se dispara EN TIEMPO REAL cada vez que la puntuación cambia")]
    public UnityEvent<float> OnScoreChanged; // NUEVO EVENTO

    private bool isLevelActive = false;
    private float spawnTimer;
    private Vector3 logicalPosition;

    private int enemiesSpawned = 0;
    private int enemiesDestroyed = 0;
    private int enemiesMissed = 0;

    void Start()
    {
        logicalPosition = transform.position;
        if (tunnel != null)
        {
            logicalPosition.x = tunnel.transform.position.x;
            logicalPosition.y = tunnel.transform.position.y;
        }

        if (autoStartLevel) StartLevel();
    }

    public void StartLevel()
    {
        if (currentLevel == null)
        {
            Debug.LogError("No hay un LevelDataSO asignado en el TargetSpawner.");
            return;
        }

        enemiesSpawned = 0;
        enemiesDestroyed = 0;
        enemiesMissed = 0;
        spawnTimer = 0f;
        isLevelActive = true;

        UpdateScoreUI(); // Reseteamos la UI al 100% al empezar

        Debug.Log("¡Nivel Iniciado! Total enemigos: " + currentLevel.totalEnemiesToSpawn);
    }

    public void ResetAndStartLevel()
    {
        isLevelActive = false;

        TargetMovement[] activeEnemies = FindObjectsOfType<TargetMovement>();
        foreach (TargetMovement enemy in activeEnemies)
        {
            Destroy(enemy.gameObject);
        }

        if (effectController != null)
        {
            effectController.ChangeEffects(new List<TunnelEffectSO>(effectController.startingEffects));
        }

        StartLevel();
    }

    void LateUpdate()
    {
        if (tunnel != null)
        {
            float maxTunnelLength = tunnel.ringCount * tunnel.ringSpacing;
            float maxAllowedZ = tunnel.transform.position.z + maxTunnelLength - 1f;
            float safeZ = Mathf.Min(logicalPosition.z, maxAllowedZ);
            logicalPosition.z = safeZ;

            Vector2 spawnerOffset = tunnel.GetCurveOffset(logicalPosition.z);
            Vector3 curvedPos = new Vector3(logicalPosition.x + spawnerOffset.x, logicalPosition.y + spawnerOffset.y, logicalPosition.z);
            transform.position = curvedPos;

            float lookAheadZ = logicalPosition.z - 1f;
            Vector2 offsetAhead = tunnel.GetCurveOffset(lookAheadZ);
            Vector3 forwardPos = new Vector3(logicalPosition.x + offsetAhead.x, logicalPosition.y + offsetAhead.y, lookAheadZ);
            Vector3 pathDirection = (forwardPos - curvedPos).normalized;

            if (pathDirection != Vector3.zero) transform.rotation = Quaternion.LookRotation(pathDirection);
        }

        if (isLevelActive && currentLevel != null)
        {
            if (enemiesSpawned < currentLevel.totalEnemiesToSpawn)
            {
                float progress = (float)enemiesSpawned / currentLevel.totalEnemiesToSpawn;
                float curveValue = currentLevel.spawnRateCurve.Evaluate(progress);
                float currentInterval = Mathf.Lerp(currentLevel.startSpawnInterval, currentLevel.endSpawnInterval, curveValue);

                spawnTimer += Time.deltaTime;

                if (spawnTimer >= currentInterval)
                {
                    SpawnOne();
                    spawnTimer = 0f;
                }
            }
            else
            {
                if (enemiesDestroyed + enemiesMissed >= currentLevel.totalEnemiesToSpawn)
                {
                    EndLevel();
                }
            }
        }
    }

    void SpawnOne()
    {
        List<Vector3> points = GetLogicalSpawnPoints(logicalPosition);
        if (points.Count == 0) return;

        Vector3 spawnPos = points[Random.Range(0, points.Count)];
        Vector3 direction = Vector3.back;

        InstantiateTarget(spawnPos, direction);

        enemiesSpawned++;
        CheckLevelEvents(enemiesSpawned);
    }

    private void CheckLevelEvents(int currentCount)
    {
        if (effectController == null) return;

        foreach (var ev in currentLevel.levelEvents)
        {
            if (ev.spawnThreshold == currentCount)
            {
                foreach (var eff in ev.effectsToAdd) effectController.AddEffect(eff);
                foreach (var eff in ev.effectsToRemove) effectController.RemoveEffect(eff);
            }
        }
    }

    public void RegisterEnemyDestroyed()
    {
        enemiesDestroyed++;
        UpdateScoreUI(); // NUEVO: Actualizamos UI
    }

    public void RegisterEnemyMissed()
    {
        enemiesMissed++;
        UpdateScoreUI(); // NUEVO: Actualizamos UI
    }

    // NUEVO: Calcula el porcentaje actual y avisa al Canvas
    private void UpdateScoreUI()
    {
        int processed = enemiesDestroyed + enemiesMissed;
        float currentAccuracy = 100f; // Por defecto empezamos al 100%

        if (processed > 0)
        {
            currentAccuracy = ((float)enemiesDestroyed / processed) * 100f;
        }

        OnScoreChanged?.Invoke(currentAccuracy);
    }

    private void EndLevel()
    {
        isLevelActive = false;

        float percentage = 0f;
        if (currentLevel.totalEnemiesToSpawn > 0)
        {
            percentage = ((float)enemiesDestroyed / currentLevel.totalEnemiesToSpawn) * 100f;
        }

        Debug.Log($"¡NIVEL COMPLETADO! Puntuación: {percentage}% ({enemiesDestroyed} aciertos, {enemiesMissed} fallos)");

        OnLevelCompleted?.Invoke(percentage);
    }

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
            movement.Initialize(logicalSpawnPos, direction, tunnel, this);
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
        Vector3 basePos = Application.isPlaying ? logicalPosition : transform.position;

        if (!Application.isPlaying && tunnel != null)
        {
            basePos.x = tunnel.transform.position.x;
            basePos.y = tunnel.transform.position.y;

            float maxTunnelLength = tunnel.ringCount * tunnel.ringSpacing;
            float maxAllowedZ = tunnel.transform.position.z + maxTunnelLength - 1f;
            basePos.z = Mathf.Min(basePos.z, maxAllowedZ);
        }

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