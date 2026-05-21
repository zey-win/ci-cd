using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FortuneOpenButtonTimer : MonoBehaviour
{
    [SerializeField] private Button openButton;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private Fortune popupPrefab;
    [SerializeField] private Canvas canvas;

    private void Awake()
    {
        if (openButton != null)
            openButton.onClick.AddListener(OpenPopup);
    }

    private void FixedUpdate()
    {
        var cm = FortuneCooldownManager.Instance;
        if (cm == null || timerText == null) return;

        if (cm.IsOnCooldown())
        {
            timerText.gameObject.SetActive(true);
            timerText.text = cm.GetRemainingText();
        }
        else
        {
            timerText.gameObject.SetActive(false);
            timerText.text = "";
        }
    }

    private void OpenPopup()
    {
        if (popupPrefab == null || canvas == null) return;
        Instantiate(popupPrefab, canvas.transform);
    }
}
