using UnityEngine;
using SmallScaleInc.TopDownPixelCharactersPack1.Combat;
using SmallScaleInc.TopDownPixelCharactersPack1.Animation;

namespace SmallScaleInc.TopDownPixelCharactersPack1.Enemies
{
    public class MeleeEnemyController : EnemyBase
    {
        [SerializeField] private int damage = 1;
        [SerializeField] private float attackRadius = 1f;
        [SerializeField] private Transform attackOrigin;
        [SerializeField] private LayerMask damageMask;
        [SerializeField] private string attackTrigger = "";

        private readonly Collider2D[] _overlapResults = new Collider2D[6];
        private Animator _animator;
    private DeathKnightAnimationController _animationController;

        private bool _missingTriggerWarned;

        protected override void Awake()
        {
            base.Awake();
            _animator = GetComponent<Animator>();
            _animationController = GetComponent<DeathKnightAnimationController>();
        }

        protected override void OnTargetAcquired()
        {
            base.OnTargetAcquired();

            if (damageMask.value == 0 && Target != null)
            {
                damageMask = 1 << Target.gameObject.layer;
            }
        }

        protected override bool TryAttack()
        {
            _animationController?.PlayAttackAnimation();

            if (_animator != null && !string.IsNullOrEmpty(attackTrigger))
            {
                if (AnimatorHasTrigger(attackTrigger))
                {
                    _animator.SetTrigger(attackTrigger);
                }
                else if (!_missingTriggerWarned)
                {
                    Debug.LogWarning($"Animator on '{name}' does not contain trigger '{attackTrigger}'.", this);
                    _missingTriggerWarned = true;
                }
            }

            Vector2 origin = attackOrigin != null ? (Vector2)attackOrigin.position : (Vector2)transform.position;
            int hitCount = Physics2D.OverlapCircleNonAlloc(origin, attackRadius, _overlapResults, ResolveDamageMask());

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D collider = _overlapResults[i];
                if (collider == null || collider.attachedRigidbody == Body)
                {
                    continue;
                }

                if (collider.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.TakeDamage(damage);
                }
            }

            return true;
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            Vector3 origin = attackOrigin != null ? attackOrigin.position : transform.position;
            Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
            Gizmos.DrawWireSphere(origin, attackRadius);
        }

        private bool AnimatorHasTrigger(string triggerName)
        {
            if (_animator == null)
            {
                return false;
            }

            foreach (var parameter in _animator.parameters)
            {
                if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == triggerName)
                {
                    return true;
                }
            }

            return false;
        }

        private LayerMask ResolveDamageMask()
        {
            if (damageMask.value != 0)
            {
                return damageMask;
            }

            Debug.LogWarning($"'{name}' has no damage mask set. Using default layer mask.", this);
            return Physics2D.DefaultRaycastLayers;
        }
    }
}
