using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PanelLoading : UICanvas
{
    [SerializeField, Min(0f)] private float spawnDuration = 0.35f;
    [SerializeField, Min(0f)] private float despawnDuration = 0.25f;
    [SerializeField, Min(0f)] private float staggerDelay = 0.08f;
    [SerializeField, Min(0f)] private float waitAfterSpawn = 0.5f;
    [SerializeField, Min(0f)] private float wiggleAngle = 12f;
    [SerializeField, Min(0f)] private float wiggleCycles = 2f;

    private readonly List<CatItem> catItems = new List<CatItem>();
    private Coroutine loadingAnimation;

    public float AnimationDuration => GetAnimationDuration();

    private class CatItem
    {
        public RectTransform Rect;
        public Vector3 OriginalScale;
        public Quaternion OriginalRotation;
    }

    public override void SetUp()
    {
        base.SetUp();
        CacheCats();
        ResetCats();
    }

    public override void Open()
    {
        base.Open();

        if (loadingAnimation != null)
        {
            StopCoroutine(loadingAnimation);
        }

        loadingAnimation = StartCoroutine(PlayLoadingAnimation());
    }

    public override void CloseDirectly()
    {
        if (loadingAnimation != null)
        {
            StopCoroutine(loadingAnimation);
            loadingAnimation = null;
        }

        base.CloseDirectly();
    }

    private void CacheCats()
    {
        catItems.Clear();

        Image[] images = GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            RectTransform rect = images[i].transform as RectTransform;
            if (rect == null || rect == transform || !images[i].name.StartsWith("Meow"))
            {
                continue;
            }

            catItems.Add(new CatItem
            {
                Rect = rect,
                OriginalScale = rect.localScale,
                OriginalRotation = rect.localRotation
            });
        }

        catItems.Sort((a, b) => b.Rect.position.y.CompareTo(a.Rect.position.y));
    }

    private void ResetCats()
    {
        for (int i = 0; i < catItems.Count; i++)
        {
            CatItem cat = catItems[i];
            cat.Rect.gameObject.SetActive(true);
            cat.Rect.localScale = Vector3.zero;
            cat.Rect.localRotation = cat.OriginalRotation;
        }
    }

    private IEnumerator PlayLoadingAnimation()
    {
        for (int i = 0; i < catItems.Count; i++)
        {
            StartCoroutine(AnimateCat(catItems[i], 0f, 1f, spawnDuration));

            if (staggerDelay > 0f && i < catItems.Count - 1)
            {
                yield return new WaitForSecondsRealtime(staggerDelay);
            }
        }

        yield return new WaitForSecondsRealtime(spawnDuration + waitAfterSpawn);

        for (int i = catItems.Count - 1; i >= 0; i--)
        {
            StartCoroutine(AnimateCat(catItems[i], 1f, 0f, despawnDuration));

            if (staggerDelay > 0f && i > 0)
            {
                yield return new WaitForSecondsRealtime(staggerDelay);
            }
        }

        yield return new WaitForSecondsRealtime(despawnDuration);
        loadingAnimation = null;
        CloseDirectly();
    }

    private IEnumerator AnimateCat(CatItem cat, float fromScale, float toScale, float duration)
    {
        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float time = Mathf.Clamp01(elapsed / duration);
            float easedTime = fromScale < toScale ? EaseOutBack(time) : Mathf.SmoothStep(0f, 1f, time);
            float scale = Mathf.LerpUnclamped(fromScale, toScale, easedTime);
            float wiggle = Mathf.Sin(time * Mathf.PI * 2f * wiggleCycles) * wiggleAngle * (1f - time);

            cat.Rect.localScale = cat.OriginalScale * scale;
            cat.Rect.localRotation = cat.OriginalRotation * Quaternion.Euler(0f, 0f, wiggle);

            yield return null;
        }

        cat.Rect.localScale = cat.OriginalScale * toScale;
        cat.Rect.localRotation = cat.OriginalRotation;
    }

    private float EaseOutBack(float time)
    {
        const float overshoot = 1.70158f;
        time -= 1f;
        return 1f + time * time * ((overshoot + 1f) * time + overshoot);
    }

    private float GetAnimationDuration()
    {
        int staggerCount = Mathf.Max(0, catItems.Count - 1);
        float spawnTime = spawnDuration + staggerDelay * staggerCount;
        float despawnTime = despawnDuration + staggerDelay * staggerCount;
        return spawnTime + waitAfterSpawn + despawnTime;
    }
}
