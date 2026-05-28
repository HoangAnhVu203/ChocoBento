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
    }

    private void OnDisable()
    {
        UnbindFreeButton();
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
} 
