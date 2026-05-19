using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class CoinFlyFX : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private RectTransform coinPrefab;
    [SerializeField, Min(0.05f)] private float flyDuration = 0.55f;
    [SerializeField, Min(0f)] private float spawnInterval = 0.025f;
    [SerializeField] private Vector2 arcOffset = new(0f, 120f);

    public float TotalDuration => flyDuration + spawnInterval * Mathf.Max(0, lastPlayCount - 1);

    private int lastPlayCount;

    public void SetRoot(RectTransform newRoot)
    {
        root = newRoot;
    }

    public void SetPrefab(RectTransform newPrefab)
    {
        coinPrefab = newPrefab;
    }

    public void Play(RectTransform from, RectTransform to, RectTransform prefab, int count)
    {
        if (from == null || to == null || count <= 0)
        {
            return;
        }

        if (prefab != null)
        {
            coinPrefab = prefab;
        }

        EnsureRoot();
        if (root == null)
        {
            return;
        }

        lastPlayCount = count;
        StartCoroutine(PlayRoutine(from, to, count));
    }

    private IEnumerator PlayRoutine(RectTransform from, RectTransform to, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (from == null || to == null || root == null)
            {
                yield break;
            }

            SpawnCoin(from, to, i);
            if (spawnInterval > 0f)
            {
                yield return new WaitForSecondsRealtime(spawnInterval);
            }
        }
    }

    private void SpawnCoin(RectTransform from, RectTransform to, int index)
    {
        if (from == null || to == null || root == null)
        {
            return;
        }

        var coin = CreateCoin();
        if (coin == null)
        {
            return;
        }

        coin.SetParent(root, false);
        coin.anchoredPosition = GetLocalPoint(from);
        coin.localScale = Vector3.one;
        coin.gameObject.SetActive(true);

        var start = coin.anchoredPosition;
        var end = GetLocalPoint(to);
        var sideOffset = new Vector2((index % 2 == 0 ? -1f : 1f) * Random.Range(12f, 42f), 0f);
        var control = (start + end) * 0.5f + arcOffset + sideOffset;
        StartCoroutine(FlyRoutine(coin, start, control, end));
    }

    private IEnumerator FlyRoutine(RectTransform coin, Vector2 start, Vector2 control, Vector2 end)
    {
        var elapsed = 0f;
        while (elapsed < flyDuration)
        {
            if (coin == null)
            {
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            var t = Mathf.Clamp01(elapsed / flyDuration);
            var eased = 1f - Mathf.Pow(1f - t, 3f);
            coin.anchoredPosition = Bezier(start, control, end, eased);
            coin.localScale = Vector3.one * Mathf.Lerp(1f, 0.65f, eased);
            yield return null;
        }

        if (coin != null)
        {
            Destroy(coin.gameObject);
        }
    }

    private RectTransform CreateCoin()
    {
        if (coinPrefab != null)
        {
            return Instantiate(coinPrefab);
        }

        var coin = new GameObject("CoinFly", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rect = coin.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(48f, 48f);
        coin.GetComponent<Image>().color = new Color(1f, 0.82f, 0.18f, 1f);
        return rect;
    }

    private Vector2 GetLocalPoint(RectTransform target)
    {
        EnsureRoot();
        if (target == null || root == null)
        {
            return Vector2.zero;
        }

        var canvas = root != null ? root.GetComponentInParent<Canvas>() : null;
        var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        var screenPoint = RectTransformUtility.WorldToScreenPoint(camera, target.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenPoint, camera, out var localPoint);
        return localPoint;
    }

    private void EnsureRoot()
    {
        if (root != null)
        {
            return;
        }

        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        root = new GameObject("FXRoot", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(canvas.transform, false);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        root.SetAsLastSibling();
    }

    private static Vector2 Bezier(Vector2 start, Vector2 control, Vector2 end, float t)
    {
        var a = Vector2.Lerp(start, control, t);
        var b = Vector2.Lerp(control, end, t);
        return Vector2.Lerp(a, b, t);
    }
}
