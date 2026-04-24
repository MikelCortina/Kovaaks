using UnityEngine;

[CreateAssetMenu(fileName = "NewTurningEffect", menuName = "Tunnel/Turning Effect", order = 3)]
public class TurningTunnelEffectSO : TunnelEffectSO
{
    [Header("Giro Continuo")]
    [Tooltip("Fuerza del giro. Positivo = Derecha, Negativo = Izquierda")]
    public float turnStrengthX = 5f;

    [Tooltip("Fuerza del giro. Positivo = Arriba, Negativo = Abajo")]
    public float turnStrengthY = 0f;

    [Tooltip("Distancia a partir de la cual el túnel empieza a doblarse (0 = empieza en la cámara)")]
    public float curveStartDistance = 15f;
}