using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    Dictionary<System.Type, UICanvas> canvasActives = new Dictionary<System.Type, UICanvas>();
    Dictionary<System.Type, UICanvas> canvasPrefabs = new Dictionary<System.Type, UICanvas>();
    [SerializeField] Transform parent;

    private void Awake()
    {
        UICanvas[] prefabs = Resources.LoadAll<UICanvas>("UI/");
        for (int i = 0; i < prefabs.Length; i++)
        {
            canvasPrefabs.Add(prefabs[i].GetType(), prefabs[i]);
        }
    }

    //mo canvas
    public T OpenUI<T>() where T : UICanvas
    {
        T canvas = GetUI<T>();

        EnsureButtonClickSounds(canvas);
        canvas.SetUp();
        canvas.Open();
        EnsureButtonClickSounds(canvas);

        return canvas;
    }

    //Dong canvas sau n time(s)
    public void CloseUI<T>(float time) where T : UICanvas
    {
        if (IsUILoaded<T>())
        {
            canvasActives[typeof(T)].Close(time);
        }
    }


    //Dong canvas truc tiep
    public UICanvas CloseUIDirectly<T>() where T : UICanvas
    {
        if (IsUILoaded<T>())
        {
            var type = typeof(T);
            var canvas = canvasActives[type];
            var shouldRemove = canvas != null && canvas.IsDestroyOnClose;

            canvas.CloseDirectly();

            if (shouldRemove)
            {
                canvasActives.Remove(type);
            }
        }
        return null;
    }

    //Kiem tra canvas da dc tao chua
    public bool IsUILoaded<T>() where T : UICanvas
    {
        return canvasActives.ContainsKey(typeof(T)) && canvasActives[typeof(T)] != null;
    }

    //Kiem tra canvas da duoc active chua
    public bool IsUIOpened<T>() where T : UICanvas
    {
        return IsUILoaded<T>() && canvasActives[typeof(T)].gameObject.activeSelf;
    }

    //Lay canvas
    public T GetUI<T>() where T : UICanvas
    {
        if (!IsUILoaded<T>())
        {
            T prefab = GetUIPrefabs<T>();
            T canvas = Instantiate(prefab, parent);
            EnsureButtonClickSounds(canvas);

            canvasActives[typeof(T)] = canvas;
        }

        return canvasActives[typeof(T)] as T;
    }


    public T GetUIPrefabs<T>() where T : UICanvas
    {
        return canvasPrefabs[typeof(T)] as T;
    }

    private void EnsureButtonClickSounds(UICanvas canvas)
    {
        if (canvas == null)
        {
            return;
        }

        var buttons = canvas.GetComponentsInChildren<UnityEngine.UI.Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null)
            {
                continue;
            }

            var clickSound = buttons[i].GetComponent<UIButtonClickSound>();
            if (clickSound == null)
            {
                clickSound = buttons[i].gameObject.AddComponent<UIButtonClickSound>();
            }

            clickSound.Bind();
        }
    }

    //Dong tat ca
    public void CloseAll()
    {
        foreach (var canvas in canvasActives.Values)
        {
            if (canvas != null && canvas.gameObject.activeSelf)
            {
                canvas.Close(0);
            }
        }
    }

    public void CloseAllDirectly()
    {
        var canvasTypes = new List<System.Type>(canvasActives.Keys);
        for (int i = 0; i < canvasTypes.Count; i++)
        {
            var type = canvasTypes[i];
            if (!canvasActives.TryGetValue(type, out var canvas))
            {
                continue;
            }

            if (canvas != null && canvas.gameObject.activeSelf)
            {
                var shouldRemove = canvas.IsDestroyOnClose;
                canvas.CloseDirectly();

                if (shouldRemove)
                {
                    canvasActives.Remove(type);
                }
            }
        }
    }
}
