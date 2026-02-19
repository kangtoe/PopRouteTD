using UnityEngine;
using UnityEngine.UI;

public class TowerButtonUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Text labelText;
    [SerializeField] private Text descriptionText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image backgroundImage;

    private static readonly Color normalColor = new Color(0.3f, 0.3f, 0.4f);
    private static readonly Color selectedColor = new Color(0.4f, 0.8f, 1f);
    private static readonly Color lockedColor = new Color(0.5f, 0.5f, 0.5f);

    public Button Button => button;

    /// <summary>구매 가능 상태: 골드 부족 시 interactable = false</summary>
    public void SetAvailable(string label, int cost, string desc = "", Sprite icon = null)
    {
        SetLabel(label);
        SetDescription(desc);
        SetIcon(icon);
        button.interactable = ResourceManager.Instance.Gold >= cost;
        SetBackgroundColor(normalColor);
    }

    /// <summary>선택 완료 상태: 하이라이트, 비활성</summary>
    public void SetSelected(string label, string desc = "", Sprite icon = null)
    {
        SetLabel(label);
        SetDescription(desc);
        SetIcon(icon);
        button.interactable = false;
        SetBackgroundColor(selectedColor);
    }

    /// <summary>잠금 상태: 회색, 비활성</summary>
    public void SetLocked(string label, string desc = "", Sprite icon = null)
    {
        SetLabel(label);
        SetDescription(desc);
        SetIcon(icon);
        button.interactable = false;
        SetBackgroundColor(lockedColor);
    }

    /// <summary>최대 레벨 도달</summary>
    public void SetMax()
    {
        SetLabel("MAX");
        SetDescription("");
        button.interactable = false;
        SetBackgroundColor(normalColor);
    }

    private void SetLabel(string text)
    {
        if (labelText != null)
            labelText.text = text;
    }

    private void SetDescription(string desc)
    {
        if (descriptionText != null)
            descriptionText.text = desc;
    }

    private void SetIcon(Sprite icon)
    {
        if (iconImage == null || icon == null) return;
        iconImage.sprite = icon;
    }

    private void SetBackgroundColor(Color color)
    {
        if (backgroundImage != null)
            backgroundImage.color = color;
    }
}
