using UnityEngine;

// Añade este script AL MODELO 3D (Hijo del EnemyCore) para darle animaciones locales 
// sin romper su movimiento perfecto a través del túnel.
public class EnemyLocalAnimator : MonoBehaviour
{
    [Header("Rotación sobre su propio eje")]
    [Tooltip("Velocidad a la que gira sobre sí mismo (Ej: Z = 300 para un taladro)")]
    public Vector3 rotationSpeed = new Vector3(0, 0, 0);

    [Header("Movimiento en Círculo / Oscilación (Wobble)")]
    [Tooltip("Velocidad de la órbita (Ej: X=3, Y=3 para hacer un círculo)")]
    public Vector3 wobbleSpeed = new Vector3(0, 0, 0);

    [Tooltip("Tamaño de la órbita en metros (Ej: X=2, Y=2 para un círculo de 2m de radio)")]
    public Vector3 wobbleAmplitude = new Vector3(0, 0, 0);

    // Guardamos la posición inicial relativa al padre
    private Vector3 initialLocalPosition;

    // Un desfase de tiempo aleatorio para que si spawnean 3 enemigos a la vez, 
    // no hagan exactamente el mismo círculo sincronizados (se vería raro).
    private float timeOffset;

    void Start()
    {
        initialLocalPosition = transform.localPosition;

        // Asignamos un punto de partida aleatorio en el tiempo (de 0 a 100 segundos)
        timeOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        // 1. ROTACIÓN (Girar como una peonza/taladro)
        if (rotationSpeed != Vector3.zero)
        {
            // Usamos Space.Self para que gire sobre su propio centro local
            transform.Rotate(rotationSpeed * Time.deltaTime, Space.Self);
        }

        // 2. OSCILACIÓN / ÓRBITA (Moverse en círculos o zig-zag alrededor del padre)
        if (wobbleAmplitude != Vector3.zero)
        {
            float t = Time.time + timeOffset;

            // Usamos Seno para X y Coseno para Y. 
            // Combinar Seno y Coseno con la misma velocidad crea un círculo perfecto.
            // Si usas solo Seno en ambos, hará una línea diagonal.
            float x = Mathf.Sin(t * wobbleSpeed.x) * wobbleAmplitude.x;
            float y = Mathf.Cos(t * wobbleSpeed.y) * wobbleAmplitude.y;
            float z = Mathf.Sin(t * wobbleSpeed.z) * wobbleAmplitude.z;

            // Aplicamos la nueva posición sumada a donde estaba originalmente
            transform.localPosition = initialLocalPosition + new Vector3(x, y, z);
        }
    }
}