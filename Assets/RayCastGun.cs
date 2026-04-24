using UnityEngine;

public class RayCastGun : MonoBehaviour
{
    public Camera playerCamera;
    public float range = 200f;
    public float hitForce = 15f;

    void Update()
    {
        // 1. DISPARO NORMAL (Clic)
        if (Input.GetButtonDown("Fire1"))
        {
            ShootStandard();
        }

        // 2. LÁSER CONTINUO (Mantener Clic) para los nuevos enemigos
        if (Input.GetButton("Fire1"))
        {
            ShootContinuousLaser();
        }
    }

    private void ShootStandard()
    {
        RaycastHit hit;
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, range))
        {
            // Buscamos EnemyCore en lugar del antiguo TargetMovement
            EnemyCore enemy = hit.collider.GetComponentInParent<EnemyCore>();
            if (enemy != null)
            {
                // Le pasamos el punto de impacto, la dirección del rayo y la fuerza
                enemy.TakeDamage(hit.point, playerCamera.transform.forward, hitForce);
            }
        }
    }

    private void ShootContinuousLaser()
    {
        RaycastHit hit;
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, range))
        {
            EnemyCore enemy = hit.collider.GetComponentInParent<EnemyCore>();
            if (enemy != null)
            {
                // Aquí le sumamos el tiempo que el láser lleva tocándole (Time.deltaTime)
                enemy.ReceiveFocus(Time.deltaTime, hit.point, playerCamera.transform.forward, hitForce);
            }
        }
    }
}