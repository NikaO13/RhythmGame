using UnityEngine;

public class BaseVisualEffect : MonoBehaviour
{
    [Header("Базовые настройки эффекта")]
    [SerializeField] protected float moveSpeed = 60f;
    [SerializeField] protected float fadeSpeed = 1.5f;
    [SerializeField] protected float lifetime = 0.6f;

    protected virtual void Update()
    {
        transform.localPosition += new Vector3(0, moveSpeed * Time.deltaTime, 0);

        if (lifetime > 0)
            lifetime -= Time.deltaTime;
        else
            ApplyFade();
    }

    protected virtual void ApplyFade() { }
}