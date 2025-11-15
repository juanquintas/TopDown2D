using System.Collections;
using UnityEngine;

namespace SmallScaleInc.TopDownPixelCharactersPack1.Animation
{
    public class DeathKnightAnimationController : EnemyAnimationBase
    {
        [SerializeField] private string attackTrigger = "Melee";
        [SerializeField] private string[] attackBoolPrefixes = { "AttackAttack", "Attack2" };
        [SerializeField] private float attackResetDelay = 0.75f;

        private Coroutine _attackResetCoroutine;

        public void PlayAttackAnimation()
        {
            TriggerIfExists(attackTrigger);

            if (attackBoolPrefixes == null || attackBoolPrefixes.Length == 0)
            {
                return;
            }

            string suffix = string.IsNullOrEmpty(CurrentDirectionSuffix) ? "East" : CurrentDirectionSuffix;
            string parameterName = null;

            foreach (var prefix in attackBoolPrefixes)
            {
                if (string.IsNullOrEmpty(prefix))
                {
                    continue;
                }

                string candidate = $"{prefix}{suffix}";
                if (!TrySetBool(candidate, true))
                {
                    continue;
                }

                parameterName = candidate;
                break;
            }

            if (string.IsNullOrEmpty(parameterName))
            {
                return;
            }

            TrySetBool("isAttackAttacking", true);

            if (_attackResetCoroutine != null)
            {
                StopCoroutine(_attackResetCoroutine);
            }

            _attackResetCoroutine = StartCoroutine(ResetAttackAfterDelay(parameterName));
        }

        private IEnumerator ResetAttackAfterDelay(string parameterName)
        {
            yield return new WaitForSeconds(attackResetDelay);
            TrySetBool(parameterName, false);
            TrySetBool("isAttackAttacking", false);
        }
    }
}
