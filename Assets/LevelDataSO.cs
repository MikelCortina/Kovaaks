using UnityEngine;
using System.Collections.Generic;

// Estructura para definir eventos en momentos exactos del nivel
[System.Serializable]
public class LevelEffectEvent
{
    [Tooltip("Número de enemigos generados en el que se activará este evento (Ej: 30)")]
    public int spawnThreshold;

    [Tooltip("Efectos que se AÑADIRÁN al túnel en este momento")]
    public List<TunnelEffectSO> effectsToAdd;

    [Tooltip("Efectos que se QUITARÁN del túnel en este momento")]
    public List<TunnelEffectSO> effectsToRemove;
}

[CreateAssetMenu(fileName = "NewLevelData", menuName = "Tunnel/Level Data", order = 10)]
public class LevelDataSO : ScriptableObject
{
    [Header("Progreso del Nivel")]
    [Tooltip("Número total de enemigos que se generarán antes de terminar el nivel")]
    public int totalEnemiesToSpawn = 100;

    [Header("Curva de Crecimiento (Dificultad)")]
    [Tooltip("Tiempo entre spawns al empezar el nivel (segundos)")]
    public float startSpawnInterval = 2.0f;
    [Tooltip("Tiempo entre spawns al final del nivel (segundos)")]
    public float endSpawnInterval = 0.4f;

    [Tooltip("Eje X = Progreso del Nivel (0 a 1). Eje Y = Multiplicador de dificultad. Una curva hacia arriba hará que salgan más rápido al final.")]
    public AnimationCurve spawnRateCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Eventos del Túnel")]
    [Tooltip("Configura aquí en qué momento del nivel el túnel girará, cambiará de color, etc.")]
    public List<LevelEffectEvent> levelEvents = new List<LevelEffectEvent>();
}