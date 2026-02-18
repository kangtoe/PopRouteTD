using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 15f;
    [SerializeField] private float lifetime = 3f;

    private int damage;
    private float splashRadius;
    private int pierceCount;
    private int remainingPierce;
    private StatusEffectType effectType;
    private float effectDuration;
    private Rigidbody2D rb;
    private float timer;
    private int enemyLayerMask;
    private bool isDeactivated;
    private readonly HashSet<int> hitInstanceIds = new();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        GetComponent<SpriteRenderer>().sortingLayerName = GameConstants.SortProjectile;
        enemyLayerMask = 1 << LayerMask.NameToLayer(GameConstants.LayerEnemy);
    }

    public void Initialize(int attackDamage, float splash = 0f,
        StatusEffectType statusEffect = StatusEffectType.None, float statusDuration = 0f,
        int pierce = 0)
    {
        damage = attackDamage;
        splashRadius = splash;
        pierceCount = pierce;
        remainingPierce = pierce;
        effectType = statusEffect;
        effectDuration = statusDuration;
        timer = lifetime;
        isDeactivated = false;
        hitInstanceIds.Clear();

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
                var enemy = hit.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage);
                    ApplyStatusEffect(enemy);
                }
            }
        }
        else if (pierceCount > 0)
        {
            int instanceId = other.gameObject.GetInstanceID();
            if (hitInstanceIds.Contains(instanceId)) return;

            var enemy = other.GetComponent<Enemy>();
            if (enemy == null) return;

            int consumed = enemy.TakeLayerDamage(remainingPierce);
            remainingPierce -= consumed;
            hitInstanceIds.Add(instanceId);
            ApplyStatusEffect(enemy);

            if (remainingPierce <= 0)
            {
                Deactivate();
                return;
            }
            return;
        }
        else
        {
            var enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                ApplyStatusEffect(enemy);
            }
        }

        Deactivate();
    }

    private void ApplyStatusEffect(Enemy enemy)
    {
        if (effectType == StatusEffectType.None) return;
        enemy.ApplyStatusEffect(effectType, effectDuration);
    }

    private void Deactivate()
    {
        if (isDeactivated) return;
        isDeactivated = true;

        rb.linearVelocity = Vector2.zero;
        ProjectilePool.Instance.Return(gameObject);
    }
}
