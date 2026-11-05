using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResetSaveButton : MonoBehaviour
{
    private const float ConfirmWindow = 3f;
    private const string IdleText = "Reset save (test)";
    private const string ConfirmText = "Sure? Click again";
    private const string DoneText = "Save reset";
    

    #if UNITY_EDITOR || DEVELOPMENT_BUILD || PLAYTEST_TOOLS
        private static readonly bool ToolsEnabled = true;
    #else
        private static readonly bool ToolsEnable = false;
    #endif

    [SerializeField] private Button button;
    [SerializeField] private TMP_Text label;

    private bool armed;
    private Coroutine resetLabel;

    void Awake()
    {
        if (!ToolsEnabled)
        {
            gameObject.SetActive(false);
            return;
        }

        button.onClick.AddListener(OnClick);
        label.text = IdleText;
    }

    void OnClick()
    {
        if (resetLabel != null) StopCoroutine(resetLabel);

        if (!armed)
        {
            armed = true;
            label.text = ConfirmText;
        }
        else
        {
            armed = false;
            DevTools.ResetEverything();
            label.text = DoneText;
        }

        resetLabel = StartCoroutine(BackToIdle());
    }

    IEnumerator BackToIdle()
    {
        yield return new WaitForSecondsRealtime(ConfirmWindow);
        armed = false;
        label.text = IdleText;
    }
}