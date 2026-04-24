using UnityEngine;

[CreateAssetMenu(fileName = "NewHighlightEffect", menuName = "Tunnel/Highlight Effect", order = 5)]
public class HighlightTunnelEffectSO : TunnelEffectSO
{
    public enum HighlightMode { Longitudinal, Transversal }

    [Header("Pintado de Vértices Específicos")]
    [Tooltip("Activa el pintado de vértices")]
    public bool useHighlight = true;

    [Tooltip("Dirección en la que se pintan las líneas")]
    public HighlightMode highlightMode = HighlightMode.Longitudinal;

    [Tooltip("El color con el que pintaremos estos vértices")]
    public Color highlightColor = Color.green;

    [Tooltip("Pinta 1 de cada X vértices o anillos.")]
    public int highlightStep = 4;
}