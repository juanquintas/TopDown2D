using System;
using UnityEngine;
using UnityEngine.Events;
using SmallScaleInc.TopDownPixelCharactersPack1.Combat;

namespace SmallScaleInc.TopDownPixelCharactersPack1.Enemies
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Health))]
    public abstract class EnemyBase : MonoBehaviour
    {
        public static event Action<EnemyBase> EnemySpawned;
        public static event Action<EnemyBase> EnemyDied;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 1.75f;
        [SerializeField] private float chaseRange = 6f;
        [SerializeField] private float attackRange = 1.25f;
        [SerializeField] private Transform[] patrolPoints;
        [SerializeField] private float patrolPointTolerance = 0.1f;

        [Header("Combat")]
        [SerializeField] private float attackCooldown = 1.25f;

        [Header("Runtime References")]
        [SerializeField] private Transform target;
        [SerializeField] private UnityEvent onDeath;

        private int _currentPatrolIndex;
        private float _lastAttackTime;
        private Rigidbody2D _rb;
        private Health _health;

        protected Rigidbody2D Body => _rb;
        protected Transform Target => target;
        protected float AttackRange => attackRange;
        protected bool CanAttack => Time.time >= _lastAttackTime + attackCooldown;
        protected Health Health => _health;

        protected virtual void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            _health = GetComponent<Health>();
            if (_health != null)
            {
                _health.Died += Die;
            }
        }

        protected virtual void Start()
        {
            if (target == null)
            {
                EnsureTarget();
            }
            else
            {
                OnTargetAcquired();
            }

        }

        protected virtual void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            EnemySpawned?.Invoke(this);
        }

        protected virtual void FixedUpdate()
        {
            if (target == null)
            {
                EnsureTarget();
            }

            if (target == null)
            {
                SetVelocity(Vector2.zero);

                return;
            }

            float distanceToTarget = Vector2.Distance(target.position, transform.position);

            if (distanceToTarget <= chaseRange)
            {
                if (distanceToTarget > attackRange)
                {
                    Vector2 direction = (target.position - transform.position).normalized;
                    Move(direction);
                }
                else
                {
                    SetVelocity(Vector2.zero);
                    HandleAttack();
                }
            }
            else
            {
                Patrol();
            }
        }

        protected void SetVelocity(Vector2 velocity)
        {
            _rb.velocity = velocity;
        }

        private void Move(Vector2 direction)
        {
            SetVelocity(direction * moveSpeed);
        }

        private void Patrol()
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                SetVelocity(Vector2.zero);
                return;
            }

            Transform patrolTarget = patrolPoints[_currentPatrolIndex];
            Vector2 direction = (patrolTarget.position - transform.position).normalized;
            Move(direction);

            if (Vector2.Distance(transform.position, patrolTarget.position) <= patrolPointTolerance)
            {
                _currentPatrolIndex = (_currentPatrolIndex + 1) % patrolPoints.Length;
            }
        }

        private void HandleAttack()
        {
            if (!CanAttack)
            {
                return;
            }

            if (TryAttack())
            {
                _lastAttackTime = Time.time;
            }
        }

        protected abstract bool TryAttack();

        protected virtual void Die()
        {
            onDeath?.Invoke();
            EnemyDied?.Invoke(this);
            Destroy(gameObject);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                OnTargetAcquired();
            }
        }

        private void EnsureTarget()
        {
            if (target != null)
            {
                return;
            }

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
                OnTargetAcquired();
            }
        }

        protected virtual void OnTargetAcquired()
        {
        }

        protected virtual void OnDisable()
        {
            if (_health != null)
            {
                _health.Died -= Die;
            }
        }

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, chaseRange);

            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.45f);
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
