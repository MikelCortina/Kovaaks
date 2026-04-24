using UnityEngine;

[CreateAssetMenu(fileName = "NewVertexWobbleEffect", menuName = "Tunnel/Vertex Wobble Effect", order = 8)]
public class VertexWobbleTunnelEffectSO : TunnelEffectSO
{
    [Header("Respiración Orgánica de Paredes (Wobble)")]
    [Tooltip("Velocidad a la que laten los vértices.")]
    public float wobbleSpeed = 3f;

    [Tooltip("Distancia máxima que los vértices se desplazan hacia adentro y hacia afuera.")]
    public float wobbleStrength = 0.5f;

    [Tooltip("Escala del caos. Valores bajos (0.5) hacen que laten zonas grandes juntas. Valores altos (5) hacen temblar vértice por vértice de forma aislada.")]
    public float wobbleNoiseScale = 2f;
}