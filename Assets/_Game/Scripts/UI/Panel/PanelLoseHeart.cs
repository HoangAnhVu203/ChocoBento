using UnityEngine;
using UnityEngine.UI;

public class PanelLoseHeart : UICanvas
{
    public enum LoseHeartAction
    {
        Replay,
        Home
    }

    private LoseHeartAction action = LoseHeartAction.Replay;

    private void OnEnable()
    {
        BindFreeButton();
        BindHeartChanged();
        RefreshSpendHeartButtons();
    }

    private void OnDisable()
    {
        UnbindFreeButton();
        if (HeartManager.Instance != null)
        {
            HeartManager.Instance.HeartChanged -= HandleHeartChanged;
        }
    }

    public void SetAction(LoseHeartAction nextAction)
    {
        action = nextAction;
    }

    public void CloseUI() => UIManager.Instance.CloseUIDirectly<PanelLoseHeart>();

    public void ReplayLevelBtn()
    {
        AgreeBtn();
    }

    public void AgreeBtn()
    {
        if (HeartManager.Instance == null || !HeartManager.Instance.TrySpendHeart())
        {
            RefreshSpendHeartButtons();
            return;
        }

        ExecuteAction();
    }

    public void FreeBtn()
    {
        ExecuteAction();
    }

    private void ExecuteAction()
    {
        UIManager.Instance.CloseUIDirectly<PanelLoseHeart>();

        if (action == LoseHeartAction.Home)
        {
            GameManager.Instance.GoHome();
            return;
        }

        GameManager.Instance.ReplayLevel();
    }

    private void BindFreeButton()
    {
        var buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (!IsFreeButton(buttons[i]))
            {
                continue;
            }

            buttons[i].onClick.RemoveListener(FreeBtn);
            buttons[i].onClick.AddListener(FreeBtn);
        }
    }

    private void BindHeartChanged()
    {
        if (HeartManager.Instance == null)
        {
            return;
        }

        HeartManager.Instance.HeartChanged -= HandleHeartChanged;
        HeartManager.Instance.HeartChanged += HandleHeartChanged;
        HeartManager.Instance.RefreshHeartDisplay();
    }

    private void HandleHeartChanged(int heartCount)
    {
        RefreshSpendHeartButtons();
    }

    private void RefreshSpendHeartButtons()
    {
        bool canSpendHeart = HeartManager.Instance != null && HeartManager.Instance.HeartCount > 0;
        var buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (IsFreeButton(buttons[i]))
            {
                continue;
            }

            if (IsSpendHeartButton(buttons[i]))
            {
                buttons[i].interactable = canSpendHeart;
            }
        }
    }

    private void UnbindFreeButton()
    {
        var buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (IsFreeButton(buttons[i]))
            {
                buttons[i].onClick.RemoveListener(FreeBtn);
            }
        }
    }

    private bool IsFreeButton(Button button)
    {
        if (button == null)
        {
            return false;
        }

        if (button.name.IndexOf("Free", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        var text = button.GetComponentInChildren<Text>(true);
        return text != null && text.text.IndexOf("Free", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private bool IsSpendHeartButton(Button button)
    {
        if (button == null)
        {
            return false;
        }

        if (button.name.IndexOf("Agree", System.StringComparison.OrdinalIgnoreCase) >= 0
            || button.name.IndexOf("Heart", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        var text = button.GetComponentInChildren<Text>(true);
        return text != null
            && (text.text.IndexOf("Agree", System.StringComparison.OrdinalIgnoreCase) >= 0
                || text.text.IndexOf("-1", System.StringComparison.OrdinalIgnoreCase) >= 0);
    }
} 
