using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("SFX")]
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip placeSound;
    [SerializeField] private AudioClip sellSound;
    [SerializeField] private AudioClip explodeSound;
    [SerializeField] private AudioClip waveStartSound;
    [SerializeField] private AudioClip upgradeSound;
    [SerializeField] private AudioClip lifeLostSound;
    [SerializeField] private AudioClip gameOverSound;
    [SerializeField] private AudioClip sceneStartSound;

    private AudioSource sfxSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
    }

    public void PlayHit() => PlaySFX(hitSound);
    public void PlayPlace() => PlaySFX(placeSound);
    public void PlaySell() => PlaySFX(sellSound);
    public void PlayExplode() => PlaySFX(explodeSound);
    public void PlayWaveStart() => PlaySFX(waveStartSound);
    public void PlayUpgrade() => PlaySFX(upgradeSound);
    public void PlayLifeLost() => PlaySFX(lifeLostSound);
    public void PlayGameOver() => PlaySFX(gameOverSound);
    public void PlaySceneStart() => PlaySFX(sceneStartSound);

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip);
    }
}
