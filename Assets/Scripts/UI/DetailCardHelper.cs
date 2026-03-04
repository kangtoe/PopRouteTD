using UnityEngine;
using UnityEngine.UI;

public static class DetailCardHelper
{
    private const float CardGap = 18f;
    private const string ContentName = "_DetailContent";

    public static TowerButtonUI Create(TowerButtonUI prefab, Transform parent)
    {
        var card = Object.Instantiate(prefab, parent);

        card.Button.enabled = false;

        var boing = card.GetComponent<UIBoingEffect>();
        if (boing != null) boing.enabled = false;

        foreach (var graphic in card.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;

        var rt = card.GetComponent<RectTransform>();

        // 기존 텍스트에서 폰트 참조 획득
        var sampleText = card.GetComponentInChildren<Text>(true);
        var font = sampleText != null ? sampleText.font : Font.CreateDynamicFontFromOSFont("Arial", 16);

        // 기존 자식 전부 비활성화
        for (int i = 0; i < rt.childCount; i++)
            rt.GetChild(i).gameObject.SetActive(false);

        // 버튼과 동일한 크기 유지

        // 단일 텍스트 (전체 영역)
        var contentObj = new GameObject(ContentName, typeof(RectTransform));
        contentObj.transform.SetParent(rt, false);
        var contentRt = contentObj.GetComponent<RectTransform>();
        contentRt.anchorMin = Vector2.zero;
        contentRt.anchorMax = Vector2.one;
        contentRt.offsetMin = new Vector2(6f, 6f);
        contentRt.offsetMax = new Vector2(-6f, -12f);
        var contentText = contentObj.AddComponent<Text>();
        contentText.font = font;
        contentText.fontSize = 18;
        contentText.fontStyle = FontStyle.Bold;
        contentText.alignment = TextAnchor.UpperCenter;
        contentText.color = Color.white;
        contentText.raycastTarget = false;

        card.gameObject.SetActive(false);
        return card;
    }

    public static void PositionLeftOf(RectTransform card, RectTransform anchor, RectTransform cardParent)
    {
        Vector3 anchorCenter = anchor.TransformPoint(anchor.rect.center);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            cardParent,
            RectTransformUtility.WorldToScreenPoint(null, anchorCenter),
            null,
            out var localPoint);

        float anchorHalfW = anchor.rect.width * anchor.lossyScale.x / cardParent.lossyScale.x * 0.5f;
        float cardHalfW = card.rect.width * 0.5f;

        card.anchoredPosition = new Vector2(
            localPoint.x - anchorHalfW - CardGap - cardHalfW,
            localPoint.y);
    }

    public static void Show(TowerButtonUI card, string title, string desc)
    {
        var rt = card.GetComponent<RectTransform>();
        var contentText = rt.Find(ContentName)?.GetComponent<Text>();

        if (contentText != null)
            contentText.text = $"<size=22>{title}</size>\n\n{desc}";

        card.gameObject.SetActive(true);
    }

    public static void Hide(TowerButtonUI card)
    {
        if (card != null)
            card.gameObject.SetActive(false);
    }
}
