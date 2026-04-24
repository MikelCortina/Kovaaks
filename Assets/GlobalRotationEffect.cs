using UnityEngine;

[CreateAssetMenu(fileName = "NewGlobalRotationEffect", menuName = "Tunnel/Global Rotation Effect", order = 7)]
public class GlobalRotationTunnelEffectSO : TunnelEffectSO
{
    [Header("Rotación Global del Túnel")]
    [Tooltip("Velocidad a la que gira todo el túnel (incluyendo sus curvas y obstáculos). Positivo = Derecha, Negativo = Izquierda")]
    public float globalRotationSpeed = 45f; // Grados por segundo
}