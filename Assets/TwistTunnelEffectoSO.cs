using UnityEngine;

[CreateAssetMenu(fileName = "NewTwistEffect", menuName = "Tunnel/Twist Effect", order = 6)]
public class TwistTunnelEffectSO : TunnelEffectSO
{
    [Header("Torsión sobre el eje Z (Sacacorchos)")]
    [Tooltip("Grados de rotación que se suman por cada metro de profundidad del túnel. Positivo = Derecha, Negativo = Izquierda")]
    public float twistDegreesPerMeter = 10f;
}