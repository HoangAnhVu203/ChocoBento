using UnityEngine;

public class PanelSetting : UICanvas
{
    [SerializeField] private GameObject homeButtonRoot;
    private bool hideHomeButton;

    public override void Open()
    {
        base.Open();
        RefreshHomeButton();
    }

    public void CloseUI()
    {
        Destroy(gameObject);
    }

    public void SetOpenedFromHome(bool openedFromHome)
    {
        hideHomeButton = openedFromHome;
        RefreshHomeButton();
    }

    public void GohomeBtn()
    {
        var loseHeartPanel = UIManager.Instance.OpenUI<PanelLoseHeart>();
        loseHeartPanel.SetAction(PanelLoseHeart.LoseHeartAction.Home);
    }

    private void RefreshHomeButton()
    {
        ResolveHomeButtonRoot();
        if (homeButtonRoot != null)
        {
            homeButtonRoot.SetActive(!hideHomeButton);
        }
    }

    private void ResolveHomeButtonRoot()
    {
        if (homeButtonRoot != null)
        {
            return;
        }

        var transforms = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name.IndexOf("Home", System.StringComparison.OrdinalIgnoreCase) >= 0
                || transforms[i].name.IndexOf("ReturnHome", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                homeButtonRoot = transforms[i].gameObject;
                return;
            }
        }
    }
}
