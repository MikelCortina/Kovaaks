using UnityEngine;

public class TargetMovement : MonoBehaviour
{
    public float speed = 5f;
    private Vector3 moveDirection;

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
        Destroy(gameObject);
    }
}