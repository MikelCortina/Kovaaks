using UnityEngine;

public class ShatterFragment : MonoBehaviour
{
    [HideInInspector] public float lifetime = 1.5f;
    [HideInInspector] public float shrinkDelay = 0.3f;

    private float timer = 0f;
    private Vector3 initialScale;

    void Start()
    {
        initialScale = transform.localScale;
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer > shrinkDelay)
        {
            float shrinkProgress = (timer - shrinkDelay) / (lifetime - shrinkDelay);
            float scale = Mathf.Lerp(1f, 0f, shrinkProgress * shrinkProgress);
            transform.localScale = initialScale * scale;
        }

        if (timer >= lifetime)
            Destroy(gameObject);
    }
}