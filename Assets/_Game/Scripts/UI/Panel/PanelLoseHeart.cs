using UnityEngine;

public class PanelLoseHeart : UICanvas
{
    public void CloseUI() => UIManager.Instance.CloseUIDirectly<PanelLoseHeart>();

    public void ReplayLevelBtn() 
    {
        GameManager.Instance.ReplayLevel();
        HeartManager.Instance.TrySpendHeart();
    }
} 