using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// WebGL 빌드에서 Linear 색공간의 밝기 차이를 자동 보정합니다.
/// </summary>
public class WebGLBrightnessCorrection : MonoBehaviour
{
    [SerializeField] private float exposureBoost = 0.2f;

    private void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        var volume = FindAnyObjectByType<Volume>();
        if (volume != null && volume.profile.TryGet(out ColorAdjustments colorAdjustments))
            colorAdjustments.postExposure.Override(colorAdjustments.postExposure.value + exposureBoost);
#endif
    }
}
