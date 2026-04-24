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

    [Header("Referencia Fallback")]
    [Tooltip("Prefab por defecto por si el nivel no tiene enemigos configurados")]
    public GameObject defaultTargetPrefab;
    public Transform playerTransform;
    public InfiniteProceduralTunnel tunnel;

    [Header("Forma y Cuadrícula")]
    public ShapeType shape = ShapeType.Rectangular;
    public float gridSpacing = 2f;
    public int rows = 3;
    public int columns = 3;
    public float circleRadius = 5f;
    public int circlePoints = 8;

    [Header("Eventos de UI")]
    public UnityEvent<float> OnLevelCompleted;
    public UnityEvent<float> OnScoreChanged;

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
        if (currentLevel == null) return;

        enemiesSpawned = 0;
        enemiesDestroyed = 0;
        enemiesMissed = 0;
        spawnTimer = 0f;
        isLevelActive = true;

        UpdateScoreUI();
    }

    public void ResetAndStartLevel()
    {
        isLevelActive = false;
        EnemyCore[] activeEnemies = FindObjectsOfType<EnemyCore>();
        foreach (EnemyCore enemy in activeEnemies) Destroy(enemy.gameObject);

        if (effectController != null) effectController.ChangeEffects(new List<TunnelEffectSO>(effectController.startingEffects));
        StartLevel();
    }

    void LateUpdate()
    {
        if (tunnel != null)
        {
            float maxTunnelLength = tunnel.ringCount * tunnel.ringSpacing;
            float maxAllowedZ = tunnel.transform.position.z + maxTunnelLength - 1f;
            logicalPosition.z = Mathf.Min(logicalPosition.z, maxAllowedZ);

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
            else if (enemiesDestroyed + enemiesMissed >= currentLevel.totalEnemiesToSpawn)
            {
                EndLevel();
            }
        }
    }

    void SpawnOne()
    {
        List<Vector3> points = GetLogicalSpawnPoints(logicalPosition);
        if (points.Count == 0) return;

        Vector3 spawnPos = points[Random.Range(0, points.Count)];
        Vector3 direction = Vector3.back;

        // NUEVO: Elegimos el prefab inteligente
        GameObject prefabToSpawn = GetRandomEnemyPrefabBasedOnProgress();

        GameObject newTarget = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
        EnemyCore core = newTarget.GetComponent<EnemyCore>();

        if (core != null)
        {
            core.Initialize(spawnPos, direction, tunnel, this);
        }

        // Mini animación de aparición (escalado)
        StartCoroutine(ScaleUpAnimation(newTarget.transform, newTarget.transform.localScale));

        enemiesSpawned++;
        CheckLevelEvents(enemiesSpawned);
    }

    // NUEVO: Lógica para elegir qué enemigo spawnear
    private GameObject GetRandomEnemyPrefabBasedOnProgress()
    {
        if (currentLevel.enemyPool.Count == 0) return defaultTargetPrefab;

        float progress = (float)enemiesSpawned / currentLevel.totalEnemiesToSpawn;
        List<EnemyPoolItem> validEnemies = new List<EnemyPoolItem>();
        float totalWeight = 0f;

        // Filtramos los que ya se han desbloqueado
        foreach (var enemy in currentLevel.enemyPool)
        {
            if (progress >= enemy.minLevelProgress)
            {
                validEnemies.Add(enemy);
                totalWeight += enemy.spawnWeight;
            }
        }

        if (validEnemies.Count == 0) return defaultTargetPrefab;

        // Ruleta aleatoria basada en pesos
        float randomValue = Random.Range(0, totalWeight);
        foreach (var enemy in validEnemies)
        {
            randomValue -= enemy.spawnWeight;
            if (randomValue <= 0f) return enemy.enemyPrefab;
        }

        return validEnemies[0].enemyPrefab;
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
        UpdateScoreUI();
    }

    public void RegisterEnemyMissed()
    {
        enemiesMissed++;
        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        int processed = enemiesDestroyed + enemiesMissed;
        float currentAccuracy = 100f;
        if (processed > 0) currentAccuracy = ((float)enemiesDestroyed / processed) * 100f;
        OnScoreChanged?.Invoke(currentAccuracy);
    }

    private void EndLevel()
    {
        isLevelActive = false;
        float percentage = 0f;
        if (currentLevel.totalEnemiesToSpawn > 0) percentage = ((float)enemiesDestroyed / currentLevel.totalEnemiesToSpawn) * 100f;
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
                for (int y = 0; y < rows; y++) points.Add(startPos + new Vector3(x * gridSpacing, y * gridSpacing, 0));
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

    IEnumerator ScaleUpAnimation(Transform t, Vector3 finalScale)
    {
        t.localScale = Vector3.zero;
        float timer = 0f;
        float dur = 0.5f;

        while (timer < dur)
        {
            if (t == null) yield break;
            timer += Time.deltaTime;
            t.localScale = Vector3.Lerp(Vector3.zero, finalScale, timer / dur);
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
            float maxAllowedZ = tunnel.transform.position.z + (tunnel.ringCount * tunnel.ringSpacing) - 1f;
            basePos.z = Mathf.Min(basePos.z, maxAllowedZ);
        }

        foreach (var p in GetLogicalSpawnPoints(basePos))
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