using UnityEngine;

public class TargetMovement : MonoBehaviour
{
    public float speed = 5f;
    private Vector3 moveDirection;

    // Propiedad pública de solo lectura
    public Vector3 MoveDirection => moveDirection;

    public void SetDirection(Vector3 direction)
    {
        moveDirection = direction;
    }

    void Update()
    {
        transform.position += moveDirection * speed * Time.deltaTime;
    }

    public void TakeDamage()
    {
        GetComponent<ShatterOnDestroy>().Shatter();
    }
}