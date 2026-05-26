using UnityEngine;

public class PanelSetting : UICanvas
{
    public void CloseUI()
    {
        Destroy(gameObject);
    }

    public void GohomeBtn()
    {
        LevelManager.Instance.GoHome();
        HeartManager.Instance.TrySpendHeart();
    }
}
