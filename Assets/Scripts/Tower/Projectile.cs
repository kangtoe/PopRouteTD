using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 15f;
    [SerializeField] private float lifetime = 3f;

    private int damage;
    private Rigidbody2D rb;
    private float timer;
    private int enemyLayerMask;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        GetComponent<SpriteRenderer>().sortingLayerName = GameConstants.SortProjectile;
        enemyLayerMask = 1 << LayerMask.NameToLayer(GameConstants.LayerEnemy);
    }

    public void Initialize(int attackDamage)
    {
        damage = attackDamage;
        timer = lifetime;

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
        if (((1 << other.gameObject.layer) & enemyLayerMask) == 0) return;

        var balloon = other.GetComponent<Balloon>();
        if (balloon == null) return;

        balloon.TakeDamage(damage);
        Deactivate();
    }

    private void Deactivate()
    {
        rb.linearVelocity = Vector2.zero;
        ProjectilePool.Instance.Return(gameObject);
    }
}
