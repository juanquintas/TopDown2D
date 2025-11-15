using UnityEngine;
using UnityEngine.Events;

namespace SmallScaleInc.TopDownPixelCharactersPack1.Combat
{
    public class Health : MonoBehaviour, IDamageable
    {
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private UnityEvent onDamaged;
    [SerializeField] private UnityEvent onDeath;

        private int _currentHealth;

        public int CurrentHealth => _currentHealth;
        public int MaxHealth => maxHealth;

    public event System.Action<int, int> Damaged; // (current, max)
    public event System.Action Died;

        private void Awake()
        {
            _currentHealth = maxHealth;
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || _currentHealth <= 0)
            {
                return;
            }

            _currentHealth = Mathf.Max(_currentHealth - amount, 0);
            onDamaged?.Invoke();
            Damaged?.Invoke(_currentHealth, maxHealth);

            if (_currentHealth == 0)
            {
                onDeath?.Invoke();
                Died?.Invoke();
            }
        }

        public void ResetHealth()
        {
            _currentHealth = maxHealth;
        }
    }
}
