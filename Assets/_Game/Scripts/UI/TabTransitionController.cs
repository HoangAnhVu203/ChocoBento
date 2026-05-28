using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class TabTransitionController : MonoBehaviour
{
    public enum Tab { Lock = 0, Home = 1, Calendar = 2 }

    [Header("Panels")]
    [SerializeField] private RectTransform panelContainer;
    [SerializeField] private GameObject leaderboardPanelGO;
    [SerializeField] private GameObject homePanelGO;
    [SerializeField] private GameObject shopPanelGO;

    [Header("Sliders")]
    [SerializeField] private UIPanelSlider leaderboardSlider;
    [SerializeField] private UIPanelSlider homeSlider;
    [SerializeField] private UIPanelSlider shopSlider;

    [Header("Start")]
    [SerializeField] private Tab startTab = Tab.Home;

    [Header("Layout")]
    [SerializeField, Range(0.5f, 1f)] private float panelStepScale = 0.7625f;

    private bool busy;
    private Tab current;
    private bool initialized;     // đã init layout chưa

    private float PanelWidth
    {
        get
        {
            float w = panelContainer != null ? panelContainer.rect.width : 0f;
            return w > 1f ? w : Screen.width;
        }
    }

    private float PanelStep => PanelWidth * panelStepScale;

    private bool IsBootTutorialPhase()
    {
        // Phase tutorial: chưa done (lần đầu) HOẶC GameManager đang bật BootTutorialActive
        // Ưu tiên dùng PlayerPrefs cho chắc chắn.
        if (GameManager.Instance == null) return false;
        if (!GameManager.Instance.IsBootTutorialDone()) return true;
        if (GameManager.Instance.BootTutorialActive) return true;
        return false;
    }

    private void Start()
    {
        // Không spawn UI trong tutorial.
        // Nhưng vẫn có thể init nếu UI đã tồn tại (ví dụ bạn đã mở Home rồi).
        TryBindExistingInstancesOnly();
        KeepBasePanelsBehindFooter();

        // Chỉ init layout nếu đủ ref (đã có panel instances)
        TryInitIfReady();
    }

    // ==== PUBLIC API: gọi sau khi bạn mở Home/Calendar/Lock xong ====
    public void RefreshAfterUIOpened()
    {
        // Sau tutorial hoặc sau khi bạn OpenUI các panel
        EnsureAndBindInstances(); // lúc này mới được phép spawn nếu cần
        KeepBasePanelsBehindFooter();
        TryInitIfReady(force: true);
    }

    public void ConfigurePanels(PanelLeaderBoard leaderboard, PanelHome home, PanelShop shop)
    {
        leaderboardPanelGO = leaderboard != null ? leaderboard.gameObject : null;
        homePanelGO = home != null ? home.gameObject : null;
        shopPanelGO = shop != null ? shop.gameObject : null;

        leaderboardSlider = leaderboard != null ? leaderboard.GetComponent<UIPanelSlider>() : null;
        homeSlider = home != null ? home.GetComponent<UIPanelSlider>() : null;
        shopSlider = shop != null ? shop.GetComponent<UIPanelSlider>() : null;

        initialized = false;
        TryInitIfReady(force: true);
    }

    public void ShowHomeImmediate()
    {
        if (!EnsureInitialized())
        {
            return;
        }

        current = Tab.Home;
        var w = PanelStep;
        ApplyStartTabSnap(current, w);
        EnsureActive(Tab.Lock, false);
        EnsureActive(Tab.Home, true);
        EnsureActive(Tab.Calendar, false);
        KeepBasePanelsBehindFooter();
    }

    // ========= Switch =========
    public void SwitchToLock() => StartCoroutine(SwitchRoutine(Tab.Lock));
    public void SwitchToHome() => StartCoroutine(SwitchRoutine(Tab.Home));
    public void SwitchToCalendar() => StartCoroutine(SwitchRoutine(Tab.Calendar));

    public void SwitchToPrevious()
    {
        if (current == Tab.Home)
        {
            SwitchToLock();
        }
        else if (current == Tab.Calendar)
        {
            SwitchToHome();
        }
    }

    public void SwitchToNext()
    {
        if (current == Tab.Lock)
        {
            SwitchToHome();
        }
        else if (current == Tab.Home)
        {
            SwitchToCalendar();
        }
    }

    private IEnumerator SwitchRoutine(Tab target)
    {
        if (busy) yield break;
        if (!EnsureInitialized()) yield break;         // chưa init thì khỏi chạy
        if (target == current) yield break;

        busy = true;
        EventSystem.current?.SetSelectedGameObject(null);

        float w = PanelStep;

        EnsureActive(current, true);
        EnsureActive(target, true);
        KeepBasePanelsBehindFooter();

        SnapLayoutForCurrent(current, w);

        int curIndex = TabIndex(current);
        int tarIndex = TabIndex(target);
        int delta = tarIndex - curIndex;

        // Slide current out
        if (current == Tab.Lock)
        {
            if (leaderboardSlider != null) leaderboardSlider.Slide(Vector2.zero, new Vector2(-delta * w, 0));
        }
        else if (current == Tab.Home)
        {
            if (homeSlider != null) homeSlider.Slide(Vector2.zero, new Vector2(-delta * w, 0));
        }
        else
        {
            if (shopSlider != null) shopSlider.Slide(Vector2.zero, new Vector2(-delta * w, 0));
        }

        // Slide target in
        if (target == Tab.Lock)
        {
            if (leaderboardSlider != null) leaderboardSlider.Slide(new Vector2(delta * w, 0), Vector2.zero);
        }
        else if (target == Tab.Home)
        {
            if (homeSlider != null) homeSlider.Slide(new Vector2(delta * w, 0), Vector2.zero);
        }
        else
        {
            if (shopSlider != null) shopSlider.Slide(new Vector2(delta * w, 0), Vector2.zero);
        }

        float wait = GetDurationForTarget(target);
        yield return new WaitForSecondsRealtime(wait);

        EnsureActive(Tab.Lock, target == Tab.Lock);
        EnsureActive(Tab.Home, target == Tab.Home);
        EnsureActive(Tab.Calendar, target == Tab.Calendar);
        KeepBasePanelsBehindFooter();

        ApplyStartTabSnap(target, w);

        current = target;
        busy = false;
        SyncFooterSelection(target, true);

        if (target == Tab.Lock)
        {
            UIManager.Instance?.GetUI<PanelLeaderBoard>()?.NotifySelectedFromTab();
        }
    }

    // ========= Init / Layout =========
    private void TryInitIfReady(bool force = false)
    {
        if (initialized && !force) return;

        // cần đủ 3 panel + slider để hoạt động ổn
        if (leaderboardPanelGO == null || homePanelGO == null || shopPanelGO == null) return;
        if (leaderboardSlider == null || homeSlider == null || shopSlider == null) return;

        initialized = true;

        current = startTab;

        if (leaderboardPanelGO != null) leaderboardPanelGO.SetActive(current == Tab.Lock);
        if (homePanelGO != null) homePanelGO.SetActive(current == Tab.Home);
        if (shopPanelGO != null) shopPanelGO.SetActive(current == Tab.Calendar);

        float w = PanelStep;

        ApplyStartTabSnap(current, w);
    }

    private bool EnsureInitialized()
    {
        if (!initialized)
        {
            TryBindExistingInstancesOnly();
            TryInitIfReady(force: true);
        }

        return initialized;
    }

    private void ApplyStartTabSnap(Tab tab, float w)
    {
        if (tab == Tab.Home)
        {
            if (homeSlider != null) homeSlider.Snap(Vector2.zero);
            if (leaderboardSlider != null) leaderboardSlider.Snap(new Vector2(-w, 0));
            if (shopSlider != null) shopSlider.Snap(new Vector2(w, 0));
        }
        else if (tab == Tab.Lock)
        {
            if (leaderboardSlider != null) leaderboardSlider.Snap(Vector2.zero);
            if (homeSlider != null) homeSlider.Snap(new Vector2(w, 0));
            if (shopSlider != null) shopSlider.Snap(new Vector2(2f * w, 0));
        }
        else
        {
            if (shopSlider != null) shopSlider.Snap(Vector2.zero);
            if (homeSlider != null) homeSlider.Snap(new Vector2(-w, 0));
            if (leaderboardSlider != null) leaderboardSlider.Snap(new Vector2(-2f * w, 0));
        }
    }

    private void SnapLayoutForCurrent(Tab cur, float w) => ApplyStartTabSnap(cur, w);

    private void EnsureActive(Tab tab, bool on)
    {
        switch (tab)
        {
            case Tab.Lock: if (leaderboardPanelGO != null) leaderboardPanelGO.SetActive(on); break;
            case Tab.Home: if (homePanelGO != null) homePanelGO.SetActive(on); break;
            case Tab.Calendar: if (shopPanelGO != null) shopPanelGO.SetActive(on); break;
        }
    }

    private int TabIndex(Tab t) => t == Tab.Lock ? -1 : (t == Tab.Home ? 0 : 1);
    private int FooterIndex(Tab t) => t == Tab.Lock ? 0 : (t == Tab.Home ? 1 : 2);

    private float GetDurationForTarget(Tab t)
    {
        if (t == Tab.Lock && leaderboardSlider != null) return leaderboardSlider.Duration;
        if (t == Tab.Home && homeSlider != null) return homeSlider.Duration;
        if (t == Tab.Calendar && shopSlider != null) return shopSlider.Duration;
        return 0.22f;
    }

    // ========= Binding =========

    // 1) Tutorial: chỉ bind cái nào đã tồn tại sẵn trong scene/canvas (KHÔNG instantiate)
    private void TryBindExistingInstancesOnly()
    {
        // tìm instance đang tồn tại
        var lockUI = FindObjectOfType<PanelLeaderBoard>(true);
        var homeUI = FindObjectOfType<PanelHome>(true);
        var calUI  = FindObjectOfType<PanelShop>(true);

        if (lockUI != null)
        {
            leaderboardPanelGO = lockUI.gameObject;
            leaderboardSlider = lockUI.GetComponent<UIPanelSlider>();
        }
        if (homeUI != null)
        {
            homePanelGO = homeUI.gameObject;
            homeSlider = homeUI.GetComponent<UIPanelSlider>();
        }
        if (calUI != null)
        {
            shopPanelGO = calUI.gameObject;
            shopSlider = calUI.GetComponent<UIPanelSlider>();
        }
    }

    // 2) Sau tutorial: được phép spawn bằng UIManager (flow cũ)
    private void EnsureAndBindInstances()
    {
        // Trong tutorial phase: không spawn
        if (IsBootTutorialPhase())
        {
            TryBindExistingInstancesOnly();
            return;
        }

        // bình thường: đảm bảo có instance
        var lockUI = UIManager.Instance.GetUI<PanelLeaderBoard>();
        var homeUI = UIManager.Instance.GetUI<PanelHome>();
        var calUI  = UIManager.Instance.GetUI<PanelShop>();

        if (lockUI != null)
        {
            leaderboardPanelGO = lockUI.gameObject;
            leaderboardSlider = lockUI.GetComponent<UIPanelSlider>();
        }
        if (homeUI != null)
        {
            homePanelGO = homeUI.gameObject;
            homeSlider = homeUI.GetComponent<UIPanelSlider>();
        }
        if (calUI != null)
        {
            shopPanelGO = calUI.gameObject;
            shopSlider = calUI.GetComponent<UIPanelSlider>();
        }
    }

    private void SyncFooterSelection(Tab tab, bool animate)
    {
        FooterTabBar footer = null;

        if (UIManager.Instance != null && UIManager.Instance.IsUILoaded<FooterTabBar>())
        {
            footer = UIManager.Instance.GetUI<FooterTabBar>();
        }

        if (footer == null)
        {
            footer = FindObjectOfType<FooterTabBar>(true);
        }

        if (footer != null)
        {
            footer.SelectFromTransition(FooterIndex(tab), animate);
        }
    }

    private void KeepBasePanelsBehindFooter()
    {
        FooterTabBar footer = null;

        if (UIManager.Instance != null && UIManager.Instance.IsUILoaded<FooterTabBar>())
            footer = UIManager.Instance.GetUI<FooterTabBar>();

        if (footer == null)
            footer = FindObjectOfType<FooterTabBar>(true);

        if (footer == null)
            return;

        PushPanelBelowFooter(leaderboardPanelGO, footer.transform);
        PushPanelBelowFooter(homePanelGO, footer.transform);
        PushPanelBelowFooter(shopPanelGO, footer.transform);
    }

    private void PushPanelBelowFooter(GameObject panelGO, Transform footerTransform)
    {
        if (panelGO == null || footerTransform == null)
            return;

        Transform panelTransform = panelGO.transform;
        if (panelTransform == footerTransform)
            return;

        if (panelTransform.parent != footerTransform.parent)
            return;

        int footerIndex = footerTransform.GetSiblingIndex();
        int panelIndex = panelTransform.GetSiblingIndex();

        if (panelIndex > footerIndex)
            panelTransform.SetSiblingIndex(footerIndex);
    }
}
