using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using SmallScaleInc.TopDownPixelCharactersPack1.Combat;
using SmallScaleInc.TopDownPixelCharactersPack1.Enemies;
using SmallScaleInc.TopDownPixelCharactersPack1.UI;
using SmallScaleInc.TopDownPixelCharactersPack1.Feedback;

namespace SmallScaleInc.TopDownPixelCharactersPack1.Systems
{
    public class LevelProgressController : MonoBehaviour
    {
        [System.Serializable]
        private class IntEvent : UnityEvent<int> { }

        [Header("Player")]
        [SerializeField] private Health playerHealth;
        [SerializeField] private bool autoFindPlayerByTag = true;
        [SerializeField] private string playerTag = "Player";

        [Header("Events")]
        [SerializeField] private UnityEvent onLevelStarted;
        [SerializeField] private UnityEvent onPlayerDamaged;
        [SerializeField] private UnityEvent onPlayerDeath;
        [SerializeField] private UnityEvent onLevelCompleted;
        [SerializeField] private IntEvent onEnemiesRemainingChanged;
        [SerializeField] private bool autoCreateHealthSummaryUI = true;

        [Header("Scene Flow")]
        [SerializeField] private bool reloadSceneOnPlayerDeath = true;
        [SerializeField] private float playerDeathDelay = 2f;
        [SerializeField] private bool loadNextSceneOnLevelComplete;
        [SerializeField] private string nextSceneName;
        [SerializeField] private float levelCompleteDelay = 1.5f;

        private readonly HashSet<EnemyBase> _aliveEnemies = new();
        private bool _playerDeathHandled;
        private bool _levelCompleteTriggered;

        private void Awake()
        {
            if (playerHealth == null && autoFindPlayerByTag)
            {
                var playerObject = GameObject.FindGameObjectWithTag(playerTag);
                if (playerObject != null)
                {
                    playerHealth = playerObject.GetComponentInChildren<Health>();
                }
            }
        }

        private void OnEnable()
        {
            EnemyBase.EnemySpawned += HandleEnemySpawned;
            EnemyBase.EnemyDied += HandleEnemyDied;
        }

        private void Start()
        {
            RegisterExistingEnemies();
            SubscribeToPlayer();
            EnsurePlayerFeedback();
            onLevelStarted?.Invoke();
            RaiseEnemiesRemainingEvent();
            EnsureHealthSummaryUI();
        }

        private void OnDisable()
        {
            EnemyBase.EnemySpawned -= HandleEnemySpawned;
            EnemyBase.EnemyDied -= HandleEnemyDied;
            UnsubscribeFromPlayer();
        }

        private void EnsureHealthSummaryUI()
        {
            if (!autoCreateHealthSummaryUI)
            {
                return;
            }

            if (FindObjectOfType<HealthSummaryUI>() != null)
            {
                return;
            }

            var uiObject = new GameObject("HealthSummaryUI");
            uiObject.transform.SetParent(transform, false);
            uiObject.AddComponent<HealthSummaryUI>();
        }

        private void EnsurePlayerFeedback()
        {
            if (playerHealth == null)
            {
                return;
            }

            if (playerHealth.GetComponent<HealthFeedback>() == null)
            {
                playerHealth.gameObject.AddComponent<HealthFeedback>();
            }
        }

        private void RegisterExistingEnemies()
        {
            var enemies = FindObjectsOfType<EnemyBase>();
            foreach (var enemy in enemies)
            {
                HandleEnemySpawned(enemy);
            }
        }

        private void SubscribeToPlayer()
        {
            if (playerHealth == null)
            {
                return;
            }

            playerHealth.Damaged += HandlePlayerDamaged;
            playerHealth.Died += HandlePlayerDied;
        }

        private void UnsubscribeFromPlayer()
        {
            if (playerHealth == null)
            {
                return;
            }

            playerHealth.Damaged -= HandlePlayerDamaged;
            playerHealth.Died -= HandlePlayerDied;
        }

        private void HandleEnemySpawned(EnemyBase enemy)
        {
            if (enemy == null || !_aliveEnemies.Add(enemy))
            {
                return;
            }

            RaiseEnemiesRemainingEvent();
        }

        private void HandleEnemyDied(EnemyBase enemy)
        {
            if (enemy != null)
            {
                _aliveEnemies.Remove(enemy);
            }

            RaiseEnemiesRemainingEvent();

            if (_levelCompleteTriggered || _aliveEnemies.Count > 0)
            {
                return;
            }

            _levelCompleteTriggered = true;
            onLevelCompleted?.Invoke();

            if (loadNextSceneOnLevelComplete)
            {
                StartCoroutine(LoadNextSceneRoutine());
            }
        }

        private void HandlePlayerDamaged(int current, int max)
        {
            onPlayerDamaged?.Invoke();
        }

        private void HandlePlayerDied()
        {
            if (_playerDeathHandled)
            {
                return;
            }

            _playerDeathHandled = true;
            onPlayerDeath?.Invoke();

            if (reloadSceneOnPlayerDeath)
            {
                StartCoroutine(ReloadSceneRoutine());
            }
        }

        private void RaiseEnemiesRemainingEvent()
        {
            onEnemiesRemainingChanged?.Invoke(_aliveEnemies.Count);
        }

        private IEnumerator ReloadSceneRoutine()
        {
            if (playerDeathDelay > 0f)
            {
                yield return new WaitForSeconds(playerDeathDelay);
            }

            var activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.name);
        }

        private IEnumerator LoadNextSceneRoutine()
        {
            if (levelCompleteDelay > 0f)
            {
                yield return new WaitForSeconds(levelCompleteDelay);
            }

            if (!string.IsNullOrWhiteSpace(nextSceneName))
            {
                SceneManager.LoadScene(nextSceneName);
                yield break;
            }

            int currentIndex = SceneManager.GetActiveScene().buildIndex;
            int nextIndex = currentIndex + 1;

            if (nextIndex < SceneManager.sceneCountInBuildSettings)
            {
                SceneManager.LoadScene(nextIndex);
            }
            else
            {
                SceneManager.LoadScene(currentIndex);
            }
        }
    }
}
