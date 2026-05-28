using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PanelEnterUserName : UICanvas
{
    public static event Action<string> UserNameSaved;

    [Header("Refs")]
    [SerializeField] private TMP_InputField userNameInput;
    [SerializeField] private Button enterButton;
    [SerializeField] private TMP_Text placeholderText;
    [SerializeField] private TMP_Text buttonLabel;

    [Header("Validation")]
    [SerializeField] private string defaultPlaceholder = "Enter your display name...";
    [SerializeField] private string submittingButtonText = "Saving...";
    [SerializeField] private Color defaultPlaceholderColor = new Color(0.19607843f, 0.19607843f, 0.19607843f, 0.5f);
    [SerializeField] private Color errorPlaceholderColor = new Color(0.9f, 0.2f, 0.2f, 1f);

    private bool isSubmitting;

    public override void SetUp()
    {
        base.SetUp();
        CacheRefsIfNeeded();
        RestoreDefaultHint();

        if (userNameInput != null)
        {
            userNameInput.text = string.Empty;
            userNameInput.onValueChanged.RemoveListener(OnInputValueChanged);
            userNameInput.onValueChanged.AddListener(OnInputValueChanged);
            userNameInput.onSubmit.RemoveListener(OnInputSubmitted);
            userNameInput.onSubmit.AddListener(OnInputSubmitted);
        }

        if (enterButton != null)
        {
            enterButton.onClick.RemoveAllListeners();
            enterButton.onClick.AddListener(SubmitUserName);
            enterButton.interactable = true;
        }

        SetButtonText("Enter");
        isSubmitting = false;
    }

    public override void Open()
    {
        base.Open();
        StartCoroutine(FocusInputNextFrame());
    }

    private void OnDestroy()
    {
        if (userNameInput != null)
        {
            userNameInput.onValueChanged.RemoveListener(OnInputValueChanged);
            userNameInput.onSubmit.RemoveListener(OnInputSubmitted);
        }
    }

    private void CacheRefsIfNeeded()
    {
        if (userNameInput == null)
        {
            userNameInput = GetComponentInChildren<TMP_InputField>(true);
        }

        if (enterButton == null)
        {
            enterButton = GetComponentInChildren<Button>(true);
        }

        if (placeholderText == null && userNameInput != null)
        {
            placeholderText = userNameInput.placeholder as TMP_Text;
        }

        if (buttonLabel == null && enterButton != null)
        {
            buttonLabel = enterButton.GetComponentInChildren<TMP_Text>(true);
        }
    }

    private IEnumerator FocusInputNextFrame()
    {
        yield return null;

        if (userNameInput == null)
        {
            yield break;
        }

        userNameInput.ActivateInputField();
        userNameInput.Select();
    }

    private void OnInputValueChanged(string _)
    {
        if (!isSubmitting)
        {
            RestoreDefaultHint();
        }
    }

    private void OnInputSubmitted(string _)
    {
        SubmitUserName();
    }

    private void SubmitUserName()
    {
        if (isSubmitting)
        {
            return;
        }

        if (LeaderBoardService.Instance == null)
        {
            ShowValidationError("Leaderboard service is unavailable.");
            return;
        }

        string rawName = userNameInput != null ? userNameInput.text : string.Empty;
        if (!LeaderBoardService.Instance.ValidateDisplayName(rawName, out _, out string validationError))
        {
            ShowValidationError(validationError);
            return;
        }

        isSubmitting = true;
        if (enterButton != null)
        {
            enterButton.interactable = false;
        }

        SetButtonText(submittingButtonText);

        LeaderBoardService.Instance.SaveDisplayName(
            rawName,
            onSuccess: HandleSaveSuccess,
            onError: HandleSaveError
        );
    }

    private void HandleSaveSuccess(string savedName)
    {
        Debug.Log($"[PanelEnterUserName] Username saved successfully: {savedName}");
        UserNameSaved?.Invoke(savedName);
        CloseDirectly();
    }

    private void HandleSaveError(string errorMessage)
    {
        isSubmitting = false;

        if (enterButton != null)
        {
            enterButton.interactable = true;
        }

        SetButtonText("Enter");
        ShowValidationError(errorMessage);
    }

    private void RestoreDefaultHint()
    {
        if (placeholderText == null)
        {
            return;
        }

        placeholderText.text = defaultPlaceholder;
        placeholderText.color = defaultPlaceholderColor;
    }

    private void ShowValidationError(string message)
    {
        Debug.LogWarning($"[PanelEnterUserName] {message}");

        if (placeholderText != null)
        {
            placeholderText.text = message;
            placeholderText.color = errorPlaceholderColor;
        }

        if (userNameInput != null)
        {
            userNameInput.text = string.Empty;
            userNameInput.ActivateInputField();
            userNameInput.Select();
        }
    }

    private void SetButtonText(string value)
    {
        if (buttonLabel != null)
        {
            buttonLabel.text = value;
        }
    }
}
