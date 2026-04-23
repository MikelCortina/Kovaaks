using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Configuración de Cámara")]
    [Tooltip("Sensibilidad del ratón. Ajústala para que coincida con tu memoria muscular.")]
    public float sensibilidad = 2.5f;

    [Tooltip("El objeto principal del jugador que rotará hacia los lados.")]
    public Transform cuerpoJugador;

    private float rotacionX = 0f;

    void Start()
    {
        // Bloqueamos el cursor en el centro de la pantalla y lo ocultamos
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // 1. OBTENER INPUT PURO (RAW)
        // Usamos GetAxisRaw en lugar de GetAxis. Esto es CRUCIAL para que se sienta
        // como Valorant, ya que elimina el suavizado artificial de Unity.
        float mouseX = Input.GetAxisRaw("Mouse X") * sensibilidad;
        float mouseY = Input.GetAxisRaw("Mouse Y") * sensibilidad;

        // NOTA: No multiplicamos por Time.deltaTime aquí. Input.GetAxis("Mouse X/Y") 
        // ya nos da la diferencia (delta) física del movimiento del ratón, por lo que 
        // ya es independiente de los FPS.

        // 2. CALCULAR ROTACIÓN VERTICAL (Pitch)
        // Restamos el input Y (si sumamos, la cámara estaría invertida)
        rotacionX -= mouseY;

        // Limitamos la cámara a 90 grados arriba y abajo para no "rompernos el cuello"
        rotacionX = Mathf.Clamp(rotacionX, -90f, 90f);

        // 3. APLICAR ROTACIÓN
        // Rotamos la cámara hacia arriba y hacia abajo
        transform.localRotation = Quaternion.Euler(rotacionX, 0f, 0f);

        // Rotamos todo el cuerpo del jugador hacia la izquierda y derecha
        cuerpoJugador.Rotate(Vector3.up * mouseX);
    }
}