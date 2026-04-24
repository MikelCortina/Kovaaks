using UnityEngine;
using TMPro; // Librería de TextMeshPro

public class LevelScoreUI : MonoBehaviour
{
    [Header("Configuración de UI")]
    public TextMeshProUGUI scoreText;

    [Tooltip("Texto que aparecerá antes del número")]
    public string prefix = "Acierto: ";

    void Start()
    {
        // Forzamos el texto inicial a 100%
        UpdatePercentage(100f);
    }

    // Esta función la llamará el Spawner cada vez que cambie la puntuación
    public void UpdatePercentage(float percentage)
    {
        if (scoreText != null)
        {
            // ToString("F1") hace que solo muestre 1 decimal (ej: 95.5%)
            scoreText.text = prefix + percentage.ToString("F1") + "%";
        }
    }
}