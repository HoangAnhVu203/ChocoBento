using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PanelResult : UICanvas
{
    [SerializeField] private Text resTxt;
    [SerializeField, Min(0f)] private float visibleDuration = 2f;
    [SerializeField, Min(0.01f)] private float scaleDuration = 0.18f;

    private Coroutine closeCoroutine;

    private void OnDisable()
    {
        if (closeCoroutine != null)
        {
            StopCoroutine(closeCoroutine);
            closeCoroutine = null;
        }
    }

    public void ShowBuyResult(bool success)
    {
        ShowMessage(success ? "Buy Success!" : "Buy Fail!");
    }

    public void ShowMessage(string message)
    {
        transform.localScale = Vector3.zero;
        ResolveText();
        if (resTxt != null)
        {
            resTxt.text = message;
        }

        if (closeCoroutine != null)
        {
            StopCoroutine(closeCoroutine);
        }

        closeCoroutine = StartCoroutine(CloseAfterDelay());
    }

    private IEnumerator CloseAfterDelay()
    {
        yield return ScaleRoutine(Vector3.zero, Vector3.one);
        yield return new WaitForSecondsRealtime(visibleDuration);
        yield return ScaleRoutine(Vector3.one, Vector3.zero);
        closeCoroutine = null;
        UIManager.Instance.CloseUIDirectly<PanelResult>();
    }

    private IEnumerator ScaleRoutine(Vector3 from, Vector3 to)
    {
        float elapsed = 0f;
        while (elapsed < scaleDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / scaleDuration);
            transform.localScale = Vector3.LerpUnclamped(from, to, EaseOutBack(t));
            yield return null;
        }

        transform.localScale = to;
    }

    private static float EaseOutBack(float t)
    {
        t = Mathf.Clamp01(t);
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private void ResolveText()
    {
        if (resTxt != null)
        {
            return;
        }

        resTxt = GetComponentInChildren<Text>(true);
    }
}
