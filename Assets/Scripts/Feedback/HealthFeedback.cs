using System.Collections;
using UnityEngine;
using SmallScaleInc.TopDownPixelCharactersPack1.Combat;

namespace SmallScaleInc.TopDownPixelCharactersPack1.Feedback
{
    public class HealthFeedback : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private AudioSource hitAudio;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color flashColor = Color.white;
        [SerializeField] private float flashDuration = 0.1f;
        [SerializeField] private GameObject hitVfxPrefab;
        [SerializeField] private Transform vfxSpawnPoint;

        private Color _originalColor = Color.white;
        private Coroutine _flashRoutine;
        private bool _subscribed;

        private void Awake()
        {
            if (health == null)
            {
                health = GetComponentInParent<Health>();
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (spriteRenderer != null)
            {
                _originalColor = spriteRenderer.color;
            }
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = _originalColor;
            }
        }

        private void Subscribe()
        {
            if (health == null || _subscribed)
            {
                return;
            }

            health.Damaged += HandleDamaged;
            health.Died += HandleDied;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (health == null || !_subscribed)
            {
                return;
            }

            health.Damaged -= HandleDamaged;
            health.Died -= HandleDied;
            _subscribed = false;
        }

        private void HandleDamaged(int current, int max)
        {
            PlayAudio();
            FlashSprite();
            SpawnVfx();
        }

        private void HandleDied()
        {
            PlayAudio();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = _originalColor;
            }
        }

        private void PlayAudio()
        {
            if (hitAudio == null)
            {
                return;
            }

            hitAudio.Play();
        }

        private void FlashSprite()
        {
            if (spriteRenderer == null || flashDuration <= 0f)
            {
                return;
            }

            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
            }

            _flashRoutine = StartCoroutine(FlashCoroutine());
        }

        private IEnumerator FlashCoroutine()
        {
            spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(flashDuration);
            spriteRenderer.color = _originalColor;
        }

        private void SpawnVfx()
        {
            if (hitVfxPrefab == null)
            {
                return;
            }

            Vector3 spawnPosition = vfxSpawnPoint != null ? vfxSpawnPoint.position : transform.position;
            Quaternion spawnRotation = vfxSpawnPoint != null ? vfxSpawnPoint.rotation : Quaternion.identity;
            Instantiate(hitVfxPrefab, spawnPosition, spawnRotation);
        }
    }
}
