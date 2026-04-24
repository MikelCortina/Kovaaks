using UnityEngine;

[CreateAssetMenu(fileName = "NewBreathingEffect", menuName = "Tunnel/Breathing Effect", order = 2)]
public class BreathingTunnelEffectSO : TunnelEffectSO
{
    [Header("Efecto de Respiración (Latido)")]
    [Tooltip("Cuánto crece y se encoge el radio del túnel")]
    public float breathingAmplitude = 1.5f;

    [Tooltip("Velocidad a la que el túnel respira")]
    public float breathingSpeed = 2f;
}