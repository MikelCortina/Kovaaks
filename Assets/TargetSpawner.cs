using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetSpawner : MonoBehaviour
{
    public enum ShapeType { Rectangular, Circular }

    [Header("Referencia")]
    public GameObject targetPrefab;
    public Transform playerTransform;

    [Header("Spawn Continuo")]
    public float initialSpawnInterval = 1.5f;
    public float minSpawnInterval = 0.2f;

    [Tooltip("Cada cuantos segundos aumenta la dificultad")]
    public float difficultyRampTime = 30f;

    [Tooltip("Curva de dificultad (X = tiempo, Y = velocidad)")]
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

    void Update()
    {
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
        List<Vector3> points = GetAllSpawnPoints();

        if (points.Count == 0) return;

        Vector3 spawnPos = points[Random.Range(0, points.Count)];

        Vector3 direction = (playerTransform.position - transform.position).normalized;

        InstantiateTarget(spawnPos, direction);
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
        else
        {
            float angleStep = 360f / circlePoints;

            for (int i = 0; i < circlePoints; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;

                points.Add(transform.position +
                    new Vector3(Mathf.Cos(angle) * circleRadius,
                                Mathf.Sin(angle) * circleRadius, 0));
            }
        }

        return points;
    }

    void InstantiateTarget(Vector3 position, Vector3 direction)
    {
        GameObject newTarget = Instantiate(targetPrefab, position, Quaternion.identity);

        TargetMovement movement = newTarget.GetComponent<TargetMovement>();

        if (movement != null)
            movement.SetDirection(direction);

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

        if (t != null)
            t.localScale = finalScale;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;

        var points = GetAllSpawnPoints();

        foreach (var p in points)
        {
            Gizmos.DrawWireSphere(p, 0.3f);
        }
    }
}