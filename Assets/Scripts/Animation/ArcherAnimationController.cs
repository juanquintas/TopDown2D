using System.Collections;
using UnityEngine;

namespace SmallScaleInc.TopDownPixelCharactersPack1.Animation
{
    public class ArcherAnimationController : EnemyAnimationBase
    {
        [SerializeField] private string attackTrigger = "Shoot";
        [SerializeField] private string[] attackBoolPrefixes = { "AttackAttack", "Attack2" };
        [SerializeField] private float attackResetDelay = 0.6f;

        private Coroutine _attackResetCoroutine;

        private static readonly Vector2 North = Vector2.up;
        private static readonly Vector2 South = Vector2.down;
        private static readonly Vector2 East = Vector2.right;
        private static readonly Vector2 West = Vector2.left;
        private static readonly Vector2 NorthEast = new Vector2(1f, 1f).normalized;
        private static readonly Vector2 NorthWest = new Vector2(-1f, 1f).normalized;
        private static readonly Vector2 SouthEast = new Vector2(1f, -1f).normalized;
        private static readonly Vector2 SouthWest = new Vector2(-1f, -1f).normalized;

        protected override void Awake()
        {
            base.Awake();
        }

        public void PlayAttackAnimation()
        {
            TriggerIfExists(attackTrigger);

            if (attackBoolPrefixes == null || attackBoolPrefixes.Length == 0)
            {
                return;
            }

            string suffix = string.IsNullOrEmpty(CurrentDirectionSuffix) ? "East" : CurrentDirectionSuffix;

            foreach (var prefix in attackBoolPrefixes)
            {
                if (string.IsNullOrEmpty(prefix))
                {
                    continue;
                }

                string parameterName = $"{prefix}{suffix}";
                if (!TrySetBool(parameterName, true))
                {
                    continue;
                }

                TrySetBool("isAttackAttacking", true);

                if (_attackResetCoroutine != null)
                {
                    StopCoroutine(_attackResetCoroutine);
                }

                _attackResetCoroutine = StartCoroutine(ResetAttackAfterDelay(parameterName));
                break;
            }
        }

        private IEnumerator ResetAttackAfterDelay(string parameterName)
        {
            yield return new WaitForSeconds(attackResetDelay);
            TrySetBool(parameterName, false);
            TrySetBool("isAttackAttacking", false);
        }

        public Vector2 GetCurrentAimDirection()
        {
            string suffix = string.IsNullOrEmpty(CurrentDirectionSuffix) ? "East" : CurrentDirectionSuffix;

            return suffix switch
            {
                "North" => North,
                "South" => South,
                "West" => West,
                "NorthEast" => NorthEast,
                "NorthWest" => NorthWest,
                "SouthEast" => SouthEast,
                "SouthWest" => SouthWest,
                _ => East
            };
        }

        public void ForceAimDirection(Vector2 direction)
        {
            FaceDirection(direction);
        }
    }
}
