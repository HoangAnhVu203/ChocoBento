using UnityEngine;
using UnityEngine.UI;

public class PanelWin : UICanvas
{
    [SerializeField] private Button nextButton;
    [SerializeField] private GameObject[] starRoots;
    [SerializeField] private RandomObjectSpawner[] randomObjectSpawners;

    private void OnEnable()
    {
        ResolveButton();
        ResolveStarRoots();
        ResolveRandomObjectSpawners();
        RefreshWinStars();
        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(HandleNextClicked);
            nextButton.onClick.AddListener(HandleNextClicked);
        }

        PlayRandomObjectSpawners();
    }

    private void OnDisable()
    {
        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(HandleNextClicked);
        }
    }

    private void HandleNextClicked()
    {
        GameManager.Instance.PlayNextLevel();
    }

    private void ResolveButton()
    {
        if (nextButton != null)
        {
            return;
        }

        nextButton = GetComponentInChildren<Button>(true);
    }

    private void ResolveRandomObjectSpawners()
    {
        if (randomObjectSpawners != null && randomObjectSpawners.Length > 0)
        {
            return;
        }

        randomObjectSpawners = GetComponentsInChildren<RandomObjectSpawner>(true);
    }

    private void ResolveStarRoots()
    {
        if (starRoots != null && starRoots.Length > 0)
        {
            return;
        }

        starRoots = new GameObject[3];
        var transforms = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name.Equals("Star1", System.StringComparison.OrdinalIgnoreCase))
            {
                starRoots[0] = transforms[i].gameObject;
            }
            else if (transforms[i].name.Equals("Star2", System.StringComparison.OrdinalIgnoreCase))
            {
                starRoots[1] = transforms[i].gameObject;
            }
            else if (transforms[i].name.Equals("Star3", System.StringComparison.OrdinalIgnoreCase))
            {
                starRoots[2] = transforms[i].gameObject;
            }
        }
    }

    private void RefreshWinStars()
    {
        if (starRoots == null)
        {
            return;
        }

        var starCount = Mathf.Clamp(GameManager.Instance.LastWinStarCount, 0, starRoots.Length);
        for (int i = 0; i < starRoots.Length; i++)
        {
            if (starRoots[i] != null)
            {
                starRoots[i].SetActive(i < starCount);
            }
        }
    }

    private void PlayRandomObjectSpawners()
    {
        if (randomObjectSpawners == null)
        {
            return;
        }

        for (int i = 0; i < randomObjectSpawners.Length; i++)
        {
            if (randomObjectSpawners[i] != null && randomObjectSpawners[i].gameObject.activeInHierarchy)
            {
                randomObjectSpawners[i].RandomShow();
            }
        }
    }
}
