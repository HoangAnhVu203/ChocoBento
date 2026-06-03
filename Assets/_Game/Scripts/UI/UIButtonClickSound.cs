using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class UIButtonClickSound : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        Bind();
    }

    private void OnEnable()
    {
        Bind();
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(PlayClickSound);
        }
    }

    public void Bind()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(PlayClickSound);
        button.onClick.AddListener(PlayClickSound);
    }

    private void PlayClickSound()
    {
        AudioManager.Instance.PlayButton();
    }
}
