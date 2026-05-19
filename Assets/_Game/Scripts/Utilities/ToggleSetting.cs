using UnityEngine;
using UnityEngine.UI;

public enum SettingType { Sound, Music, Vibration }

public class ToggleSetting : MonoBehaviour
{
    public SettingType settingType;

    [Header("UI")]
    public Toggle toggle;
    public RectTransform checkmark;

    [Header("Images")]
    public Image backgroundImage;
    public Image checkmarkImage;

    [Header("Sprites")]
    public Sprite backgroundOnSprite;
    public Sprite backgroundOffSprite;

    public Sprite checkmarkOnSprite;
    public Sprite checkmarkOffSprite;

    [Header("Position")]
    public Vector2 leftPos = new Vector2(-30, 0);
    public Vector2 rightPos = new Vector2(200, 0);

    void Start()
    {
        toggle.onValueChanged.AddListener(OnToggleChanged);

        // Load trạng thái từ PlayerPrefs
        bool isOn = LoadState();

        // Không trigger event khi set giá trị ban đầu
        toggle.SetIsOnWithoutNotify(isOn);

        ApplyVisual(isOn);
    }

    bool LoadState()
    {
        switch (settingType)
        {
            case SettingType.Sound:
                return PlayerPrefs.GetInt("SOUND_ON", 1) == 1;

            case SettingType.Music:
                return PlayerPrefs.GetInt("MUSIC_ON", 1) == 1;

            case SettingType.Vibration:
                return PlayerPrefs.GetInt("VIBRATION_ON", 1) == 1;
        }

        return true;
    }

    void SaveState(bool isOn)
    {
        switch (settingType)
        {
            case SettingType.Sound:
                PlayerPrefs.SetInt("SOUND_ON", isOn ? 1 : 0);
                break;

            case SettingType.Music:
                PlayerPrefs.SetInt("MUSIC_ON", isOn ? 1 : 0);

                // Ví dụ:
                // if (AudioManager.Instance.musicSource)
                //     AudioManager.Instance.musicSource.mute = !isOn;

                break;

            case SettingType.Vibration:
                PlayerPrefs.SetInt("VIBRATION_ON", isOn ? 1 : 0);
                break;
        }

        PlayerPrefs.Save();
    }

    void OnToggleChanged(bool isOn)
    {
        ApplyVisual(isOn);
        SaveState(isOn);
    }

    void ApplyVisual(bool isOn)
    {
        // Move checkmark
        checkmark.anchoredPosition = isOn ? rightPos : leftPos;

        // Change background sprite
        if (backgroundImage != null)
        {
            backgroundImage.sprite = isOn
                ? backgroundOnSprite
                : backgroundOffSprite;
        }

        // Change checkmark sprite
        if (checkmarkImage != null)
        {
            checkmarkImage.sprite = isOn
                ? checkmarkOnSprite
                : checkmarkOffSprite;
        }
    }
}