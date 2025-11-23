using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using SmallScaleInc.TopDownPixelCharactersPack1.Combat;
using SmallScaleInc.TopDownPixelCharactersPack1.Feedback;

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
        [Header("Death")]
        [SerializeField] private string fallbackDeathTrigger = "Die";
        [SerializeField] private string fallbackDeathBool = "isDead";
        [SerializeField] private float fallbackDeathDelay = 1.25f;
        [SerializeField] private bool autoAddHealthFeedback = true;

        private int _currentPatrolIndex;
        private float _lastAttackTime;
        private Rigidbody2D _rb;
        private Health _health;
        private DeathHandler _deathHandler;
        private Animator _animator;
        private Coroutine _fallbackDeathRoutine;

        protected Rigidbody2D Body => _rb;
        protected Transform Target => target;
        protected float AttackRange => attackRange;
        protected bool CanAttack => Time.time >= _lastAttackTime + attackCooldown;
        protected Health Health => _health;
        protected Animator CachedAnimator => _animator;
        protected int BuildTargetLayerMask()
        {
            if (target == null)
            {
                return 0;
            }

            int mask = 1 << target.gameObject.layer;
            foreach (var collider in target.GetComponentsInChildren<Collider2D>())
            {
                mask |= 1 << collider.gameObject.layer;
            }

            return mask;
        }

        protected virtual void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            _health = GetComponent<Health>();
            _deathHandler = GetComponent<DeathHandler>();
            _animator = GetComponent<Animator>();
            if (_health != null)
            {
                _health.Died += Die;
            }

            if (autoAddHealthFeedback && GetComponent<HealthFeedback>() == null)
            {
                gameObject.AddComponent<HealthFeedback>();
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
            enabled = false;
            SetVelocity(Vector2.zero);
            onDeath?.Invoke();
            EnemyDied?.Invoke(this);
            if (_deathHandler != null && _deathHandler.TryHandleDeath())
            {
                return;
            }

            DisableCollisionForDeath();
            if (TryPlayFallbackDeath())
            {
                return;
            }

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

            if (_fallbackDeathRoutine != null)
            {
                StopCoroutine(_fallbackDeathRoutine);
                _fallbackDeathRoutine = null;
            }
        }

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, chaseRange);

            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.45f);
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }

        private void DisableCollisionForDeath()
        {
            foreach (var collider in GetComponentsInChildren<Collider2D>())
            {
                collider.enabled = false;
            }

            if (_rb != null)
            {
                _rb.velocity = Vector2.zero;
                _rb.angularVelocity = 0f;
                _rb.simulated = false;
            }
        }

        private bool TryPlayFallbackDeath()
        {
            if (_animator == null)
            {
                return false;
            }

            bool triggered = false;

            if (!string.IsNullOrEmpty(fallbackDeathTrigger) && AnimatorHasParameter(fallbackDeathTrigger, AnimatorControllerParameterType.Trigger))
            {
                _animator.ResetTrigger(fallbackDeathTrigger);
                _animator.SetTrigger(fallbackDeathTrigger);
                triggered = true;
            }

            if (!string.IsNullOrEmpty(fallbackDeathBool) && AnimatorHasParameter(fallbackDeathBool, AnimatorControllerParameterType.Bool))
            {
                _animator.SetBool(fallbackDeathBool, true);
                triggered = true;
            }

            if (!triggered)
            {
                return false;
            }

            if (_fallbackDeathRoutine != null)
            {
                StopCoroutine(_fallbackDeathRoutine);
            }

            _fallbackDeathRoutine = StartCoroutine(FallbackDeathRoutine());
            return true;
        }

        private IEnumerator FallbackDeathRoutine()
        {
            if (fallbackDeathDelay > 0f)
            {
                yield return new WaitForSeconds(fallbackDeathDelay);
            }

            _fallbackDeathRoutine = null;
            Destroy(gameObject);
        }

        private bool AnimatorHasParameter(string parameterName, AnimatorControllerParameterType type)
        {
            if (_animator == null)
            {
                return false;
            }

            foreach (var parameter in _animator.parameters)
            {
                if (parameter.type == type && parameter.name == parameterName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
