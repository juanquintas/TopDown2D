using UnityEngine;
using SmallScaleInc.TopDownPixelCharactersPack1.Combat;
using SmallScaleInc.TopDownPixelCharactersPack1.Animation;

namespace SmallScaleInc.TopDownPixelCharactersPack1.Enemies
{
    public class RangedEnemyController : EnemyBase
    {
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private float projectileSpeed = 8f;
        [SerializeField] private Transform firePoint;
        [SerializeField] private LayerMask lineOfSightMask;
        [SerializeField] private string fireTrigger = "Shoot";
        [SerializeField] private LayerMask projectileCollisionMask;

        private Collider2D _collider;
        private ArcherAnimationController _animationController;

        protected override void Awake()
        {
            base.Awake();
            _collider = GetComponent<Collider2D>();
            _animationController = GetComponent<ArcherAnimationController>();
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

            if (lineOfSightMask.value == 0)
            {
                lineOfSightMask = targetMask;
            }
            else
            {
                lineOfSightMask |= targetMask;
            }

            if (projectileCollisionMask.value == 0)
            {
                projectileCollisionMask = targetMask;
            }
            else
            {
                projectileCollisionMask |= targetMask;
            }
        }

        protected override bool TryAttack()
        {
            if (projectilePrefab == null || Target == null)
            {
                return false;
            }

            Vector2 origin = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
            Vector2 targetDirection = ((Vector2)Target.position - origin).normalized;

            if (_animationController != null)
            {
                _animationController.ForceAimDirection(targetDirection);
            }

            Vector2 aimDirection = targetDirection;
            if (_animationController != null)
            {
                var animationDirection = _animationController.GetCurrentAimDirection();
                if (animationDirection.sqrMagnitude > Mathf.Epsilon)
                {
                    aimDirection = animationDirection;
                }
            }

            if (aimDirection.sqrMagnitude < Mathf.Epsilon)
            {
                aimDirection = targetDirection;
            }

            origin += aimDirection * 0.1f;

            if (!HasLineOfSight(origin, targetDirection))
            {
                return false;
            }

            Projectile projectileInstance = Instantiate(projectilePrefab, origin, Quaternion.identity);

            if (projectileCollisionMask.value != 0)
            {
                projectileInstance.SetCollisionMask(projectileCollisionMask);
            }

            if (_collider != null)
            {
                projectileInstance.SetOwnerCollider(_collider);
            }

            projectileInstance.Initialize(aimDirection, projectileSpeed);

            if (CachedAnimator != null && !string.IsNullOrEmpty(fireTrigger))
            {
                foreach (var parameter in CachedAnimator.parameters)
                {
                    if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == fireTrigger)
                    {
                        CachedAnimator.SetTrigger(fireTrigger);
                        break;
                    }
                }
            }

            _animationController?.PlayAttackAnimation();

            return true;
        }

        private bool HasLineOfSight(Vector2 origin, Vector2 direction)
        {
            if (lineOfSightMask.value == 0)
            {
                return true;
            }

            float maxDistance = AttackRange + 0.5f;
            var hits = Physics2D.RaycastAll(origin, direction, maxDistance, lineOfSightMask);
            foreach (var hit in hits)
            {
                if (hit.collider == null)
                {
                    continue;
                }

                if (hit.collider.transform == transform)
                {
                    continue;
                }

                return hit.collider.transform == Target;
            }

            return false;
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            if (firePoint == null)
            {
                return;
            }

            Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.45f);
            Gizmos.DrawLine(firePoint.position, firePoint.position + firePoint.right * AttackRange);
        }

    }
}
