using UnityEngine;

[CreateAssetMenu(fileName = "NewTunnelEffect", menuName = "Tunnel/Effect", order = 1)]
public class TunnelEffectSO : ScriptableObject
{
    [Header("Estructura")]
    public float baseRadius = 25f;
    public float speed = 75f;

    [Header("Irregularidad")]
    public float noiseScale =25f;
    public float noiseStrength = 25f;

    [Header("Curvas")]
    [Tooltip("Intensidad de la curva en el eje X")]
    public float curveX = 0f;
    [Tooltip("Intensidad de la curva en el eje Y")]
    public float curveY = 0f;

    [Header("Color")]
    [Tooltip("Gradiente de color a lo largo de la profundidad del túnel")]
    public Gradient tunnelColors;
}