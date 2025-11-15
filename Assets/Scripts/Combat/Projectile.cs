using UnityEngine;

namespace SmallScaleInc.TopDownPixelCharactersPack1.Combat
{
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float speed = 6f;
        [SerializeField] private int damage = 1;
        [SerializeField] private float lifetime = 5f;
        [SerializeField] private LayerMask collisionMask = ~0;

    private Vector2 _direction = Vector2.right;
    private Rigidbody2D _rb;
    private Collider2D _collider;
    private Collider2D _ownerCollider;
    private bool _initialized;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();
        }

        private void OnEnable()
        {
            if (!_initialized)
            {
                return;
            }

            Launch();
            if (lifetime > 0f)
            {
                Invoke(nameof(Dispose), lifetime);
            }
        }

        public void Initialize(Vector2 direction, float overrideSpeed = -1f)
        {
            _direction = direction.sqrMagnitude > Mathf.Epsilon ? direction.normalized : Vector2.right;
            if (overrideSpeed > 0f)
            {
                speed = overrideSpeed;
            }

            _initialized = true;
            Launch();

            if (lifetime > 0f)
            {
                CancelInvoke(nameof(Dispose));
                Invoke(nameof(Dispose), lifetime);
            }
        }

        public void SetCollisionMask(LayerMask mask)
        {
            collisionMask = mask;
        }

        public void SetOwnerCollider(Collider2D ownerCollider)
        {
            if (_ownerCollider == ownerCollider)
            {
                return;
            }

            RestoreOwnerCollision();

            _ownerCollider = ownerCollider;

            if (_collider != null && _ownerCollider != null)
            {
                Physics2D.IgnoreCollision(_collider, _ownerCollider, true);
            }
        }

        private void Launch()
        {
            if (_rb != null)
            {
                _rb.velocity = _direction * speed;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_ownerCollider != null && other == _ownerCollider)
            {
                return;
            }

            if (((1 << other.gameObject.layer) & collisionMask.value) == 0)
            {
                return;
            }

            if (other.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(damage);
            }

            Dispose();
        }

        private void Dispose()
        {
            CancelInvoke();
            RestoreOwnerCollision();
            Destroy(gameObject);
        }

        private void RestoreOwnerCollision()
        {
            if (_collider != null && _ownerCollider != null)
            {
                Physics2D.IgnoreCollision(_collider, _ownerCollider, false);
            }

            _ownerCollider = null;
        }
    }
}
