using System.Collections;
using UnityEngine;

public class RandomObjectSpawner : MonoBehaviour
{
    [Header("Objects")]
    [SerializeField] private GameObject[] objects;

    [Header("Random Loop")]
    [SerializeField] private float showDuration = 0.75f;

    Coroutine loopCoroutine;
    GameObject currentObject;

    void OnDisable()
    {
        StopRandomLoop();
    }

    [ContextMenu("Random Show")]
    public void RandomShow()
    {
        if (objects == null || objects.Length == 0)
            return;

        StopRandomLoop();
        loopCoroutine = StartCoroutine(RandomLoopRoutine());
    }

    public void StopRandomLoop()
    {
        if (loopCoroutine != null)
        {
            StopCoroutine(loopCoroutine);
            loopCoroutine = null;
        }

        SetCurrentObject(null);
    }

    IEnumerator RandomLoopRoutine()
    {
        while (true)
        {
            var nextObject = PickRandomObject();
            yield return ShowScaleRoutine(nextObject);
            SetCurrentObject(null);
        }
    }

    GameObject PickRandomObject()
    {
        if (objects == null || objects.Length == 0)
        {
            return null;
        }

        if (objects.Length == 1)
        {
            return objects[0];
        }

        GameObject nextObject = null;
        for (int i = 0; i < 8; i++)
        {
            nextObject = objects[Random.Range(0, objects.Length)];
            if (nextObject != currentObject)
            {
                return nextObject;
            }
        }

        return nextObject;
    }

    void SetCurrentObject(GameObject nextObject)
    {
        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
            {
                objects[i].SetActive(objects[i] == nextObject);
            }
        }

        currentObject = nextObject;
    }

    IEnumerator ShowScaleRoutine(GameObject obj)
    {
        if (obj == null)
        {
            yield break;
        }

        SetCurrentObject(obj);

        var transformCache = obj.transform;
        transformCache.localScale = Vector3.zero;

        var time = 0f;
        var duration = Mathf.Max(0.001f, showDuration);
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            var t = Mathf.Clamp01(time / duration);
            t = Mathf.SmoothStep(0f, 1f, t);
            transformCache.localScale = Vector3.LerpUnclamped(Vector3.zero, Vector3.one, t);
            yield return null;
        }

        transformCache.localScale = Vector3.one;
    }
}
