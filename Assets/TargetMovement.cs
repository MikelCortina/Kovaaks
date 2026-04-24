using UnityEngine;

public class TargetMovement : MonoBehaviour
{
    public float speed = 5f;
    private Vector3 moveDirection;

    private Vector3 basePosition;
    private InfiniteProceduralTunnel tunnelRef;
    private TargetSpawner spawnerRef; // Referencia al Spawner para mandar la puntuación

    private bool hasBeenProcessed = false; // Evita puntuar dos veces

    public Vector3 MoveDirection => moveDirection;

    // NUEVO: Añadimos el TargetSpawner al Initialize
    public void Initialize(Vector3 startPos, Vector3 direction, InfiniteProceduralTunnel tunnel, TargetSpawner spawner)
    {
        basePosition = startPos;
        moveDirection = direction;
        tunnelRef = tunnel;
        spawnerRef = spawner;

        UpdateVisualPosition();
    }

    void LateUpdate()
    {
        basePosition += moveDirection * speed * Time.deltaTime;
        UpdateVisualPosition();

        // Si el cubo ha pasado por detrás de la cámara del jugador (se considera fallo)
        if (!hasBeenProcessed && spawnerRef != null)
        {
            if (basePosition.z < spawnerRef.playerTransform.position.z - 2f)
            {
                hasBeenProcessed = true;
                spawnerRef.RegisterEnemyMissed();
                Destroy(gameObject); // Lo destruimos para limpiar memoria
            }
        }
    }

    private void UpdateVisualPosition()
    {
        if (tunnelRef != null)
        {
            Vector2 offset = tunnelRef.GetCurveOffset(basePosition.z);
            Vector3 finalPos = new Vector3(basePosition.x + offset.x, basePosition.y + offset.y, basePosition.z);

            transform.position = finalPos;

            float lookAheadZ = basePosition.z - 1f;
            Vector2 offsetAhead = tunnelRef.GetCurveOffset(lookAheadZ);
            Vector3 finalPosAhead = new Vector3(basePosition.x + offsetAhead.x, basePosition.y + offsetAhead.y, lookAheadZ);

            Vector3 pathDirection = (finalPosAhead - finalPos).normalized;
            if (pathDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(pathDirection);
            }
        }
        else
        {
            transform.position = basePosition;
        }
    }

    // Esta es la función a la que llamas cuando le disparas
    public void TakeDamage()
    {
        if (!hasBeenProcessed)
        {
            hasBeenProcessed = true;
            if (spawnerRef != null) spawnerRef.RegisterEnemyDestroyed();

            GetComponent<ShatterOnDestroy>().Shatter(); // Tu script de rotura
            // Asumo que tu script Shatter destruye o desactiva este GameObject
        }
    }
}