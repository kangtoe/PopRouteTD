using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// Slider를 통해 밝기(Post Exposure)를 조절합니다.
/// </summary>
public class BrightnessController : MonoBehaviour
{
    [SerializeField] private Slider brightnessSlider;

    private ColorAdjustments colorAdjustments;

    private void Start()
    {
        var volume = FindAnyObjectByType<Volume>();
        if (volume != null)
            volume.profile.TryGet(out colorAdjustments);

        if (brightnessSlider != null)
        {
            brightnessSlider.onValueChanged.AddListener(SetBrightness);
            SetBrightness(brightnessSlider.value);
        }
    }

    private void OnDestroy()
    {
        if (brightnessSlider != null)
            brightnessSlider.onValueChanged.RemoveListener(SetBrightness);
    }

    private void SetBrightness(float value)
    {
        if (colorAdjustments != null)
            colorAdjustments.postExposure.Override(value);
    }
}
