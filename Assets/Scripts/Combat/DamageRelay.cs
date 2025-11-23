using UnityEngine;

namespace SmallScaleInc.TopDownPixelCharactersPack1.Combat
{
    public class DamageRelay : MonoBehaviour, IDamageable
    {
        [SerializeField] private Health targetHealth;
        [SerializeField] private bool autoAssignInParents = true;

        private void Awake()
        {
            if (targetHealth == null && autoAssignInParents)
            {
                targetHealth = GetComponentInParent<Health>();
            }
        }

        private void OnValidate()
        {
            if (targetHealth == null && autoAssignInParents)
            {
                targetHealth = GetComponentInParent<Health>();
            }
        }

        public void TakeDamage(int amount)
        {
            if (targetHealth == null)
            {
                return;
            }

            targetHealth.TakeDamage(amount);
        }
    }
}
