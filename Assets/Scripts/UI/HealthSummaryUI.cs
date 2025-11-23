using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using SmallScaleInc.TopDownPixelCharactersPack1.Combat;
using SmallScaleInc.TopDownPixelCharactersPack1.Enemies;

namespace SmallScaleInc.TopDownPixelCharactersPack1.UI
{
    public class HealthSummaryUI : MonoBehaviour
    {
        private sealed class TrackedEntry
        {
            public Health Health;
            public bool IsPlayer;
            public string DisplayName;
            public System.Action DeathHandler;
        }

        [SerializeField] private Vector2 panelAnchorMin = new(0.02f, 0.75f);
        [SerializeField] private Vector2 panelAnchorMax = new(0.35f, 0.98f);
        [SerializeField] private Color panelColor = new(0f, 0f, 0f, 0.65f);
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private int fontSize = 16;

        private readonly Dictionary<Health, TrackedEntry> _tracked = new();
        private readonly StringBuilder _builder = new();

        private Canvas _canvas;
        private RectTransform _panel;
        private Text _summaryText;
        private bool _dirty;

        private void Awake()
        {
            CreateCanvas();
            CreatePanel();
            CreateText();
        }

        private void OnEnable()
        {
            EnemyBase.EnemySpawned += HandleEnemySpawned;
            EnemyBase.EnemyDied += HandleEnemyDied;
            MarkDirty();
        }

        private void Start()
        {
            TrackPlayer();
            RegisterExistingEnemies();
        }

        private void OnDisable()
        {
            EnemyBase.EnemySpawned -= HandleEnemySpawned;
            EnemyBase.EnemyDied -= HandleEnemyDied;

            foreach (var entry in _tracked.Values)
            {
                if (entry.Health != null)
                {
                    entry.Health.Damaged -= HandleTrackedDamaged;
                    if (entry.DeathHandler != null)
                    {
                        entry.Health.Died -= entry.DeathHandler;
                    }
                }
            }

            _tracked.Clear();
        }

        private void Update()
        {
            if (_dirty)
            {
                RefreshText();
                _dirty = false;
            }
        }

        private void CreateCanvas()
        {
            var canvasGo = new GameObject("HealthSummaryCanvas");
            canvasGo.transform.SetParent(transform, false);

            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 500;
            _canvas.pixelPerfect = true;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();
        }

        private void CreatePanel()
        {
            var panelGo = new GameObject("HealthSummaryPanel");
            panelGo.transform.SetParent(_canvas.transform, false);
            _panel = panelGo.AddComponent<RectTransform>();
            _panel.anchorMin = panelAnchorMin;
            _panel.anchorMax = panelAnchorMax;
            _panel.offsetMin = Vector2.zero;
            _panel.offsetMax = Vector2.zero;

            var image = panelGo.AddComponent<Image>();
            image.color = panelColor;
        }

        private void CreateText()
        {
            var textGo = new GameObject("HealthSummaryText");
            textGo.transform.SetParent(_panel, false);
            var rect = textGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.05f, 0.05f);
            rect.anchorMax = new Vector2(0.95f, 0.95f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _summaryText = textGo.AddComponent<Text>();
            _summaryText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _summaryText.alignment = TextAnchor.UpperLeft;
            _summaryText.color = textColor;
            _summaryText.fontSize = fontSize;
            _summaryText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _summaryText.verticalOverflow = VerticalWrapMode.Overflow;
            _summaryText.text = "";
        }

        private void TrackPlayer()
        {
            var playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject == null)
            {
                return;
            }

            var playerHealth = playerObject.GetComponentInChildren<Health>();
            if (playerHealth == null)
            {
                return;
            }

            TrackHealth(playerHealth, "Jugador", true);
        }

        private void RegisterExistingEnemies()
        {
            var enemies = FindObjectsOfType<EnemyBase>();
            foreach (var enemy in enemies)
            {
                HandleEnemySpawned(enemy);
            }
        }

        private void HandleEnemySpawned(EnemyBase enemy)
        {
            if (enemy == null)
            {
                return;
            }

            var health = enemy.GetComponent<Health>();
            if (health == null)
            {
                return;
            }

            TrackHealth(health, enemy.name, false);
        }

        private void HandleEnemyDied(EnemyBase enemy)
        {
            if (enemy == null)
            {
                return;
            }

            var health = enemy.GetComponent<Health>();
            if (health == null)
            {
                return;
            }

            UntrackHealth(health);
        }

        private void TrackHealth(Health health, string displayName, bool isPlayer)
        {
            if (health == null || _tracked.ContainsKey(health))
            {
                return;
            }

            var entry = new TrackedEntry
            {
                Health = health,
                DisplayName = displayName,
                IsPlayer = isPlayer
            };

            health.Damaged += HandleTrackedDamaged;

            System.Action deathHandler = null;
            deathHandler = () =>
            {
                health.Died -= deathHandler;
                UntrackHealth(health);
            };

            health.Died += deathHandler;
            entry.DeathHandler = deathHandler;

            _tracked.Add(health, entry);
            MarkDirty();
        }

        private void UntrackHealth(Health health)
        {
            if (health == null || !_tracked.TryGetValue(health, out var entry))
            {
                return;
            }

            health.Damaged -= HandleTrackedDamaged;
            if (entry.DeathHandler != null)
            {
                health.Died -= entry.DeathHandler;
            }

            _tracked.Remove(health);
            MarkDirty();
        }

        private void HandleTrackedDamaged(int current, int max)
        {
            MarkDirty();
        }

        private void RefreshText()
        {
            if (_summaryText == null)
            {
                return;
            }

            _builder.Clear();

            foreach (var entry in GetOrderedEntries())
            {
                if (entry.Health == null)
                {
                    continue;
                }

                _builder.Append(entry.DisplayName);
                _builder.Append(':');
                _builder.Append(' ');
                _builder.Append(Mathf.Max(0, entry.Health.CurrentHealth));
                _builder.Append(' ');
                _builder.Append('/');
                _builder.Append(' ');
                _builder.Append(entry.Health.MaxHealth);
                _builder.Append('\n');
            }

            _summaryText.text = _builder.ToString();
        }

        private IEnumerable<TrackedEntry> GetOrderedEntries()
        {
            foreach (var entry in _tracked.Values)
            {
                if (entry.IsPlayer)
                {
                    yield return entry;
                }
            }

            foreach (var entry in _tracked.Values)
            {
                if (!entry.IsPlayer)
                {
                    yield return entry;
                }
            }
        }

        private void MarkDirty()
        {
            _dirty = true;
        }
    }
}
