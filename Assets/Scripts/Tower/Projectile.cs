using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 15f;
    [SerializeField] private float lifetime = 3f;

    private int damage;
    private float splashRadius;
    private StatusEffectType effectType;
    private float effectDuration;
    private Rigidbody2D rb;
    private float timer;
    private int enemyLayerMask;
    private bool isDeactivated;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        GetComponent<SpriteRenderer>().sortingLayerName = GameConstants.SortProjectile;
        enemyLayerMask = 1 << LayerMask.NameToLayer(GameConstants.LayerEnemy);
    }

    public void Initialize(int attackDamage, float splash = 0f,
        StatusEffectType statusEffect = StatusEffectType.None, float statusDuration = 0f)
    {
        damage = attackDamage;
        splashRadius = splash;
        effectType = statusEffect;
        effectDuration = statusDuration;
        timer = lifetime;
        isDeactivated = false;

        rb.rotation = transform.eulerAngles.z;
        rb.linearVelocity = transform.up * speed;

        gameObject.SetActive(true);
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            Deactivate();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDeactivated) return;
        if (((1 << other.gameObject.layer) & enemyLayerMask) == 0) return;

        if (splashRadius > 0f)
        {
            ExplosionEffect.Spawn(transform.position, splashRadius);
            var hits = Physics2D.OverlapCircleAll(transform.position, splashRadius, enemyLayerMask);
            foreach (var hit in hits)
            {
                var balloon = hit.GetComponent<Balloon>();
                if (balloon != null)
                {
                    balloon.TakeDamage(damage);
                    ApplyStatusEffect(balloon);
                }
            }
        }
        else
        {
            var balloon = other.GetComponent<Balloon>();
            if (balloon != null)
            {
                balloon.TakeDamage(damage);
                ApplyStatusEffect(balloon);
            }
        }

        Deactivate();
    }

    private void ApplyStatusEffect(Balloon balloon)
    {
        if (effectType == StatusEffectType.None) return;
        balloon.ApplyStatusEffect(effectType, effectDuration);
    }

    private void Deactivate()
    {
        if (isDeactivated) return;
        isDeactivated = true;

        rb.linearVelocity = Vector2.zero;
        ProjectilePool.Instance.Return(gameObject);
    }
}
