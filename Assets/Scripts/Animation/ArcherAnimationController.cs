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
    }
}
