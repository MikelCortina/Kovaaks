using UnityEngine;

public class LevelRestartButton : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El spawner que controla el progreso del nivel")]
    public TargetSpawner levelManager;

    [Tooltip("Tecla opcional para reiniciar rápidamente mientras pruebas")]
    public KeyCode quickRestartKey = KeyCode.R;

    void Update()
    {
        // Atajo de teclado para reiniciar rápidamente sin darle al botón UI
        if (Input.GetKeyDown(quickRestartKey))
        {
            RestartLevel();
        }
    }

    // Esta es la función que debes llamar desde el OnClick() de tu botón UI
    public void RestartLevel()
    {
        if (levelManager != null)
        {
            levelManager.ResetAndStartLevel();
        }
        else
        {
            Debug.LogError("No has asignado el TargetSpawner al botón de reinicio.");
        }
    }
}