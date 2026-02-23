using UnityEngine;

/// <summary>
/// 적에게 보잉보잉한 느낌의 squash &amp; stretch 애니메이션을 적용합니다.
/// Enemy 프리팹에 붙여두면 자동으로 동작합니다.
/// </summary>
public class BoingAnimation : MonoBehaviour
{
    [SerializeField] private float frequency = 1f;   // 초당 사이클 수
    [SerializeField] private float amplitude = 0.02f;  // 스케일 변화량 (0 ~ 0.2 권장)


    private Vector3 baseScale;
    private float timeOffset;

    private void Awake()
    {
        baseScale = transform.localScale;
    }

    private void OnEnable()
    {
        // 오브젝트 풀에서 꺼낼 때마다 위상을 새로 랜덤해서 적마다 다르게 움직임
        timeOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        float t = Mathf.Sin(Time.time * frequency * Mathf.PI * 2f + timeOffset);

        // squash & stretch: Y가 늘어날 때 X는 약간 줄어들고, 반대도 마찬가지
        float scaleY = baseScale.y * (1f + amplitude * t);
        float scaleX = baseScale.x * (1f - amplitude * 0.5f * t);

        transform.localScale = new Vector3(scaleX, scaleY, baseScale.z);
    }
}
