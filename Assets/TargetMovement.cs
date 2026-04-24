using UnityEngine;

public class TargetMovement : MonoBehaviour
{
    public float speed = 5f;
    private Vector3 moveDirection;

    private Vector3 basePosition;
    private InfiniteProceduralTunnel tunnelRef;

    public Vector3 MoveDirection => moveDirection;

    public void Initialize(Vector3 startPos, Vector3 direction, InfiniteProceduralTunnel tunnel)
    {
        basePosition = startPos;
        moveDirection = direction;
        tunnelRef = tunnel;

        // NUEVO: Calculamos la curva y rotación en el frame 0. 
        // Así nunca lo verás atravesar la pared al spawnear.
        UpdateVisualPosition();
    }

    void Update()
    {
        // 1. Avanzamos la posición lógica (recta)
        basePosition += moveDirection * speed * Time.deltaTime;

        // 2. Aplicamos la curva
        UpdateVisualPosition();
    }

    private void UpdateVisualPosition()
    {
        if (tunnelRef != null)
        {
            // Posición actual curvada
            Vector2 offset = tunnelRef.GetCurveOffset(basePosition.z);
            Vector3 finalPos = new Vector3(basePosition.x + offset.x, basePosition.y + offset.y, basePosition.z);

            transform.position = finalPos;

            // Rotación mirando hacia el siguiente punto de la curva
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

    public void TakeDamage()
    {
        GetComponent<ShatterOnDestroy>().Shatter();
    }
}