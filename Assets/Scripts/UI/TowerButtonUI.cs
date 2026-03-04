using UnityEngine;
using UnityEngine.UI;

public class TowerButtonUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Text labelText;
    [SerializeField] private Text costText;
    [SerializeField] private Text descriptionText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private GameObject maxImage;

    public Button Button => button;

    /// <summary>구매 가능 상태: 골드 부족 시 interactable = false</summary>
    public void SetAvailable(string label, int cost, string desc = "", Sprite icon = null)
    {
        SetLabel(label);
        SetCost(cost);
        SetDescription(desc);
        SetIcon(icon);
        button.interactable = ResourceManager.Instance.Gold >= cost;
        SetBackgroundColor(GameConstants.UIColorNormal);
        SetMaxImageActive(false);
        SetHoldProgress(0);
    }

    /// <summary>선택 완료 상태: 하이라이트, 비활성</summary>
    public void SetSelected(string label, string desc = "", Sprite icon = null)
    {
        SetLabel(label);
        SetCost(-1);
        SetDescription(desc);
        SetIcon(icon);
        button.interactable = false;
        SetBackgroundColor(GameConstants.UIColorNormal);
        SetMaxImageActive(true);
        SetHoldProgress(0);
    }

    /// <summary>확인 대기 상태: 주황색, 한 번 더 클릭하면 실행</summary>
    public void SetConfirm(string label, int cost, string desc = "", Sprite icon = null)
    {
        SetLabel(label);
        SetCost(cost);
        SetDescription(desc);
        SetIcon(icon);
        button.interactable = true;
        SetBackgroundColor(GameConstants.UIColorActive);
        SetMaxImageActive(false);
        SetHoldProgress(0);
    }

    /// <summary>잠금 상태: 회색, 비활성</summary>
    public void SetLocked(string label, string desc = "", Sprite icon = null)
    {
        SetLabel(label);
        SetCost(-1);
        SetDescription(desc);
        SetIcon(icon);
        button.interactable = false;
        SetBackgroundColor(GameConstants.UIColorLocked);
        SetMaxImageActive(false);
        SetHoldProgress(0);
    }

    /// <summary>최대 레벨 도달</summary>
    public void SetMax(string label, Sprite icon = null)
    {
        SetLabel(label);
        SetCost(-1);
        SetDescription("");
        SetIcon(icon);
        button.interactable = false;
        SetBackgroundColor(GameConstants.UIColorNormal);
        SetMaxImageActive(true);
        SetHoldProgress(0);
    }

    private void SetLabel(string text)
    {
        if (labelText != null)
            labelText.text = text;
    }

    private void SetCost(int cost)
    {
        if (costText != null)
            costText.text = cost >= 0 ? $"{cost}" : "";
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

    public void SetDragging(bool dragging)
    {
        SetBackgroundColor(dragging ? GameConstants.UIColorActive : GameConstants.UIColorNormal);
    }

    [SerializeField] private Image holdFillImage;

    public void SetHoldProgress(float t)
    {
        if (holdFillImage == null) return;

        if (t > 0f)
        {
            holdFillImage.color = GameConstants.UIColorActive;
            holdFillImage.fillAmount = Mathf.Clamp01(t);
            holdFillImage.gameObject.SetActive(true);
        }
        else
        {
            holdFillImage.fillAmount = 0f;
            holdFillImage.gameObject.SetActive(false);
        }
    }

    private void SetMaxImageActive(bool active)
    {
        if (maxImage != null)
            maxImage.SetActive(active);
    }
}
