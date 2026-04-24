using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class LevelEffectEvent
{
    public int spawnThreshold;
    public List<TunnelEffectSO> effectsToAdd;
    public List<TunnelEffectSO> effectsToRemove;
}

// NUEVO: Estructura para definir qué enemigos pueden salir en este nivel
[System.Serializable]
public class EnemyPoolItem
{
    [Tooltip("El Prefab de este enemigo")]
    public GameObject enemyPrefab;

    [Tooltip("Probabilidad de que salga. A mayor número, más frecuente.")]
    public float spawnWeight = 10f;

    [Range(0f, 1f)]
    [Tooltip("¿En qué momento del nivel se desbloquea? 0 = desde el principio. 0.5 = a la mitad del nivel.")]
    public float minLevelProgress = 0f;
}

[CreateAssetMenu(fileName = "NewLevelData", menuName = "Tunnel/Level Data", order = 10)]
public class LevelDataSO : ScriptableObject
{
    [Header("Progreso del Nivel")]
    public int totalEnemiesToSpawn = 100;

    [Header("Tipos de Enemigos Disponibles")]
    public List<EnemyPoolItem> enemyPool = new List<EnemyPoolItem>();

    [Header("Curva de Crecimiento (Dificultad)")]
    public float startSpawnInterval = 2.0f;
    public float endSpawnInterval = 0.4f;
    public AnimationCurve spawnRateCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Eventos del Túnel")]
    public List<LevelEffectEvent> levelEvents = new List<LevelEffectEvent>();
}