using UnityEngine;
using UnityEngine.UI;
using SmallScaleInc.TopDownPixelCharactersPack1.Combat;

namespace SmallScaleInc.TopDownPixelCharactersPack1.UI
{
    public class HealthBarUI : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private Slider slider;
        [SerializeField] private Image fillImage;
        [SerializeField] private Gradient fillGradient;
        [SerializeField] private bool hideWhenFull = true;

        private bool _subscribed;

        private void Awake()
        {
            if (health == null)
            {
                health = GetComponentInParent<Health>();
            }

            InitializeSlider();
        }

        private void OnValidate()
        {
            if (health == null)
            {
                health = GetComponentInParent<Health>();
            }

            if (slider == null)
            {
                slider = GetComponentInChildren<Slider>();
            }

            if (fillImage == null && slider != null && slider.fillRect != null)
            {
                fillImage = slider.fillRect.GetComponent<Image>();
            }
        }

        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
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

        private void InitializeSlider()
        {
            if (slider == null || health == null)
            {
                return;
            }

            slider.maxValue = health.MaxHealth;
            slider.value = health.CurrentHealth;
            UpdateFillColor(slider.normalizedValue);
            slider.gameObject.SetActive(!hideWhenFull || slider.value < slider.maxValue);
        }

        private void Refresh()
        {
            if (slider == null)
            {
                return;
            }

            if (health != null)
            {
                slider.maxValue = health.MaxHealth;
                slider.value = health.CurrentHealth;
                UpdateFillColor(slider.normalizedValue);
                slider.gameObject.SetActive(!hideWhenFull || slider.value < slider.maxValue);
            }
            else
            {
                slider.gameObject.SetActive(false);
            }
        }

        private void HandleDamaged(int current, int max)
        {
            UpdateSlider(current, max);
        }

        private void HandleDied()
        {
            UpdateSlider(0, health != null ? health.MaxHealth : 0);
        }

        private void UpdateSlider(int current, int max)
        {
            if (slider == null)
            {
                return;
            }

            slider.maxValue = max;
            slider.value = current;
            UpdateFillColor(slider.normalizedValue);
            slider.gameObject.SetActive(!hideWhenFull || current < max);
        }

        private void UpdateFillColor(float normalizedValue)
        {
            if (fillImage == null || fillGradient == null)
            {
                return;
            }

            fillImage.color = fillGradient.Evaluate(normalizedValue);
        }
    }
}
