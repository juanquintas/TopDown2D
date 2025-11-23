using System.Collections;
using UnityEngine;
using SmallScaleInc.TopDownPixelCharactersPack1.Animation;

namespace SmallScaleInc.TopDownPixelCharactersPack1.Combat
{
    public class DeathHandler : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Health health;
        [SerializeField] private string deathTrigger = "Die";
        [SerializeField] private string deathBool = "isDead";
        [SerializeField] private float destroyDelay = 1.25f;
        [SerializeField] private Behaviour[] behavioursToDisable;
        [SerializeField] private Collider2D[] collidersToDisable;
        [SerializeField] private bool disableRigidBody = true;
        [SerializeField] private bool disableAllBehaviours = true;
        [SerializeField] private bool autoHandleHealthDeath = true;

        private bool _isProcessing;

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (health == null)
            {
                health = GetComponent<Health>();
            }
        }

        private void OnEnable()
        {
            if (!autoHandleHealthDeath || health == null)
            {
                return;
            }

            health.Died += HandleHealthDeath;
        }

        private void OnDisable()
        {
            if (!autoHandleHealthDeath || health == null)
            {
                return;
            }

            health.Died -= HandleHealthDeath;
        }

        private void HandleHealthDeath()
        {
            TryHandleDeath();
        }

        public bool TryHandleDeath()
        {
            if (_isProcessing)
            {
                return true;
            }

            _isProcessing = true;
            DisableBehaviours();
            DisableColliders();
            StopRigidBody();
            PlayAnimation();
            StartCoroutine(DestroyAfterDelay());
            return true;
        }

        private void DisableBehaviours()
        {
            DisableExplicitBehaviours();
            DisableOtherBehaviours();

            var animationController = GetComponent<EnemyAnimationBase>();
            if (animationController != null)
            {
                animationController.enabled = false;
            }
        }

        private void DisableExplicitBehaviours()
        {
            if (behavioursToDisable == null)
            {
                behavioursToDisable = System.Array.Empty<Behaviour>();
            }

            foreach (var behaviour in behavioursToDisable)
            {
                if (behaviour == null)
                {
                    continue;
                }

                behaviour.enabled = false;
            }
        }

        private void DisableOtherBehaviours()
        {
            if (!disableAllBehaviours)
            {
                return;
            }

            foreach (var behaviour in GetComponents<Behaviour>())
            {
                if (behaviour == null || behaviour == this || behaviour == animator || behaviour is AudioBehaviour)
                {
                    continue;
                }

                behaviour.enabled = false;
            }
        }

        private void DisableColliders()
        {
            if (collidersToDisable != null && collidersToDisable.Length > 0)
            {
                foreach (var collider in collidersToDisable)
                {
                    if (collider == null)
                    {
                        continue;
                    }

                    collider.enabled = false;
                }

                return;
            }

            foreach (var collider in GetComponentsInChildren<Collider2D>())
            {
                collider.enabled = false;
            }
        }

        private void StopRigidBody()
        {
            if (!disableRigidBody)
            {
                return;
            }

            if (!TryGetComponent<Rigidbody2D>(out var rb))
            {
                return;
            }

            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }

        private void PlayAnimation()
        {
            if (animator == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(deathTrigger))
            {
                animator.ResetTrigger(deathTrigger);
                animator.SetTrigger(deathTrigger);
            }

            if (!string.IsNullOrEmpty(deathBool))
            {
                animator.SetBool(deathBool, true);
            }
        }

        private IEnumerator DestroyAfterDelay()
        {
            if (destroyDelay > 0f)
            {
                yield return new WaitForSeconds(destroyDelay);
            }

            Destroy(gameObject);
        }
    }
}
