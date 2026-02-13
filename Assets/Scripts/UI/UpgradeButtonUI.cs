using UnityEngine;
using UnityEngine.UI;

public class UpgradeButtonUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Text buttonText;
    [SerializeField] private Text descriptionText;
    [SerializeField] private Image buttonImage;

    private static readonly Color normalColor = new Color(0.3f, 0.3f, 0.4f);
    private static readonly Color selectedColor = new Color(0.4f, 0.8f, 1f);
    private static readonly Color lockedColor = new Color(0.5f, 0.5f, 0.5f);

    public Button Button => button;

    /// <summary>구매 가능 상태: 이름 + 비용 + 설명 표시</summary>
    public void SetAvailable(string label, int cost, string desc = "")
    {
        buttonText.text = $"{label} ({cost})";
        SetDescription(desc);
        button.interactable = ResourceManager.Instance.Energy >= cost;
        buttonImage.color = normalColor;
    }

    /// <summary>선택 완료 상태: 하이라이트</summary>
    public void SetSelected(string label, string desc = "")
    {
        buttonText.text = label;
        SetDescription(desc);
        button.interactable = false;
        buttonImage.color = selectedColor;
    }

    /// <summary>잠금 상태: 회색 비활성</summary>
    public void SetLocked(string label, string desc = "")
    {
        buttonText.text = label;
        SetDescription(desc);
        button.interactable = false;
        buttonImage.color = lockedColor;
    }

    /// <summary>최대 레벨 도달</summary>
    public void SetMax()
    {
        buttonText.text = "MAX";
        SetDescription("");
        button.interactable = false;
        buttonImage.color = normalColor;
    }

    private void SetDescription(string desc)
    {
        if (descriptionText != null)
            descriptionText.text = desc;
    }
}
