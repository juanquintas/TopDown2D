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
        private DeathKnightAnimationController _animationController;

        private bool _missingTriggerWarned;

        protected override void Awake()
        {
            base.Awake();
            _animationController = GetComponent<DeathKnightAnimationController>();
        }

        protected override void OnTargetAcquired()
        {
            base.OnTargetAcquired();

            if (Target == null)
            {
                return;
            }

            int targetMask = BuildTargetLayerMask();
            if (targetMask == 0)
            {
                return;
            }

            damageMask = damageMask.value == 0 ? targetMask : damageMask | targetMask;
        }

        protected override bool TryAttack()
        {
            _animationController?.PlayAttackAnimation();

            if (CachedAnimator != null && !string.IsNullOrEmpty(attackTrigger))
            {
                if (AnimatorHasTrigger(attackTrigger))
                {
                    CachedAnimator.SetTrigger(attackTrigger);
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

                if (!TryResolveDamageable(collider, out var damageable))
                {
                    continue;
                }

                damageable.TakeDamage(damage);
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
            var animator = CachedAnimator;
            if (animator == null)
            {
                return false;
            }

            foreach (var parameter in animator.parameters)
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

        private bool TryResolveDamageable(Collider2D collider, out IDamageable damageable)
        {
            if (collider.TryGetComponent(out damageable))
            {
                return true;
            }

            damageable = collider.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                return true;
            }

            damageable = collider.GetComponentInChildren<IDamageable>();
            return damageable != null;
        }
    }
}
