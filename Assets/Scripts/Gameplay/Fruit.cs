using System.Collections;
using UnityEngine;

public class Fruit : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    public int colorId;
    private Coroutine animCo;

    public void ConfigFruit(Sprite fruitSprite, int color)
    {
        colorId = color;
        spriteRenderer.sprite = fruitSprite;
    }

    /// <summary>
    /// Fruit bị "đẩy ra" rồi despawn.
    /// </summary>
    public void SqueezeOut(Vector3 step, int orderIndex)
    {
        Play(animCo, SqueezeOutCoroutine(step, orderIndex));
    }

    /// <summary>
    /// Fruit còn lại dịch chuyển để lấp chỗ trống (không despawn).
    /// </summary>
    public void Shift(Vector3 offset, int steps)
    {
        if (steps <= 0) return;
        Play(animCo, ShiftCoroutine(offset, steps));
    }

    private void Play(Coroutine current, IEnumerator next)
    {
        if (current != null) StopCoroutine(current);
        animCo = StartCoroutine(next);
    }

    private IEnumerator SqueezeOutCoroutine(Vector3 offset, int orderIndex)
    {

        Vector3 start = transform.localPosition;

        // Đẩy ra theo hướng ngược step một đoạn nhỏ
        // (tuỳ game bạn có thể đổi target = start - step * 1f hoặc +step)
        Vector3 target = start - offset * orderIndex;

        float dur = Mathf.Max(0.01f, GlobalValues.fruitSqueezeOutDuration * orderIndex);
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            transform.localPosition = Vector3.LerpUnclamped(start, target, t);
            yield return null;
        }

        transform.localPosition = target;

        // Despawn về pool
        animCo = null;
        PoolManager.Instance.Despawn(gameObject);
    }

    private IEnumerator ShiftCoroutine(Vector3 offset, int steps)
    {
        Vector3 start = transform.localPosition;
        Vector3 target = start - offset * steps;

        float dur = Mathf.Max(0.01f, GlobalValues.fruitSqueezeOutDuration * steps);
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            transform.localPosition = Vector3.LerpUnclamped(start, target, t);
            yield return null;
        }

        transform.localPosition = target;
        animCo = null;
    }

    private void OnDisable()
    {
        // an toàn khi object bị despawn trong lúc đang chạy coroutine
        if (animCo != null)
        {
            StopCoroutine(animCo);
            animCo = null;
        }
    }
}
