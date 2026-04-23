using UnityEngine;

public class RaycastGun : MonoBehaviour
{
    [Header("Configuración del Arma")]
    public float damage = 10f;
    public float range = 100f;
    public Camera fpsCamera; // Arrastra tu cámara principal aquí

    // Opcional: Efectos
    public ParticleSystem muzzleFlash;

    public AudioSource audioSource;
    public AudioClip shootSound;

    public AudioClip hitMarkerSound;

    void Update()
    {
        // Detecta el clic izquierdo del ratón
        if (Input.GetButtonDown("Fire1"))
        {
            Shoot();
        }
    }

    void Shoot()
    {
        // Reproduce el destello del disparo si tienes uno
        if (muzzleFlash != null) muzzleFlash.Play();

        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }

        RaycastHit hit;

        // Trazamos el rayo desde el centro exacto de la cámara (la mira del jugador)
        // hacia adelante
        if (Physics.Raycast(fpsCamera.transform.position, fpsCamera.transform.forward, out hit, range))
        {
            // Comprobamos si lo que hemos golpeado tiene el script de movimiento/vida
            TargetMovement target = hit.transform.GetComponent<TargetMovement>();

            if (target != null)
            {
                // Si es un objetivo válido, lo destruimos
                target.TakeDamage();

                if (audioSource != null && hitMarkerSound != null)
                {
                    audioSource.PlayOneShot(hitMarkerSound);
                }
            }

            // Aquí puedes añadir un efecto de impacto (chispas, agujeros de bala) usando hit.point y hit.normal
        }
    }
}