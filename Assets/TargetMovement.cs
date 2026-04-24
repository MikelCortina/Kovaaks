using UnityEngine;

public class EnemyCore : MonoBehaviour
{
    public enum HealthType { OneHitKill, FocusLaser }

    [Header("Configuración de Vida y Daño")]
    public HealthType healthType = HealthType.OneHitKill;
    public float requiredFocusTime = 1.5f;
    private float currentFocusTime = 0f;

    [Header("Movimiento")]
    public float speed = 5f;

    private Vector3 moveDirection;
    private Vector3 basePosition;
    private InfiniteProceduralTunnel tunnelRef;
    private TargetSpawner spawnerRef;
    private bool isDeadOrMissed = false;

    public Vector3 MoveDirection => moveDirection;

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
        if (isDeadOrMissed) return;

        basePosition += moveDirection * speed * Time.deltaTime;
        UpdateVisualPosition();

        if (spawnerRef != null && basePosition.z < spawnerRef.playerTransform.position.z - 2f)
        {
            isDeadOrMissed = true;
            spawnerRef.RegisterEnemyMissed();
            Destroy(gameObject);
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
            if (pathDirection != Vector3.zero) transform.rotation = Quaternion.LookRotation(pathDirection);
        }
        else
        {
            transform.position = basePosition;
        }
    }

    public void TakeDamage(Vector3 impactPoint, Vector3 impactDir, float force)
    {
        if (isDeadOrMissed) return;
        if (healthType == HealthType.OneHitKill) Die(impactPoint, impactDir, force);
    }

    public void ReceiveFocus(float deltaTime, Vector3 impactPoint, Vector3 impactDir, float force)
    {
        if (isDeadOrMissed) return;

        if (healthType == HealthType.FocusLaser)
        {
            currentFocusTime += deltaTime;
            if (currentFocusTime >= requiredFocusTime) Die(impactPoint, impactDir, force);
        }
        else if (healthType == HealthType.OneHitKill)
        {
            Die(impactPoint, impactDir, force);
        }
    }

    private void Die(Vector3 impactPoint, Vector3 impactDir, float force)
    {
        isDeadOrMissed = true;
        if (spawnerRef != null) spawnerRef.RegisterEnemyDestroyed();

        // NUEVO: Buscamos el script de romper en los hijos (donde está la malla)
        ShatterOnDestroy shatter = GetComponentInChildren<ShatterOnDestroy>();
        if (shatter != null)
        {
            shatter.Shatter(impactPoint, impactDir, force);

            // Una vez que el hijo se rompe en pedazos, destruimos todo el objeto Padre
            Destroy(gameObject, 0.05f);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}