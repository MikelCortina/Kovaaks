using UnityEngine;

[CreateAssetMenu(fileName = "NewColorCycleEffect", menuName = "Tunnel/Color Cycle Effect", order = 4)]
public class ColorCycleTunnelEffectSO : TunnelEffectSO
{
    [Header("Animación de Color")]
    [Tooltip("Velocidad a la que los colores fluyen por el túnel. Valores positivos mueven el color hacia el jugador, negativos se alejan.")]
    public float colorCycleSpeed = 1f;
}