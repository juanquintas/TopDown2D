using UnityEngine;
using System.Collections;

namespace SmallScaleInc.TopDownPixelCharactersPack1
{
    public class PlayerController : MonoBehaviour
    {
        public float speed = 2.0f;
        private Rigidbody2D rb;
        private Vector2 movementDirection;
        private bool isOnStairs = false;
        public bool isCrouching = false;
        private SpriteRenderer spriteRenderer;
        private float lastAngle;
        private int baseSortingLayerId;
        private int baseSortingOrder;

        public bool isRanged;
        public bool isStealth;
        public GameObject projectilePrefab;
        public GameObject AoEPrefab;

        public float projectileSpeed = 10.0f;
        public float shootDelay = 0.5f;

        public Vector2 CurrentMovementDirection => movementDirection;
        public float CurrentFacingAngle => lastAngle;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                baseSortingLayerId = spriteRenderer.sortingLayerID;
                baseSortingOrder = spriteRenderer.sortingOrder;
            }
        }

        void Update()
        {
            HandleMovementInput();

            if (Input.GetKeyDown(KeyCode.C))
            {
                HandleCrouching();
            }

            if (isRanged)
            {
                if (Input.GetMouseButtonDown(1))
                {
                    Invoke(nameof(DelayedShoot), shootDelay);
                }

                if (Input.GetKeyDown(KeyCode.Alpha5))
                {
                    StartCoroutine(Quickshot());
                }

                if (Input.GetKeyDown(KeyCode.Alpha6))
                {
                    StartCoroutine(CircleShot());
                }

                if (Input.GetKeyDown(KeyCode.Alpha3))
                {
                    StartCoroutine(DeployAoEDelayed());
                }
            }
        }

        void FixedUpdate()
        {
            if (movementDirection == Vector2.zero)
            {
                return;
            }

            rb.MovePosition(rb.position + movementDirection * speed * Time.fixedDeltaTime);
        }

        void LateUpdate()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            spriteRenderer.sortingLayerID = baseSortingLayerId;
            spriteRenderer.sortingOrder = baseSortingOrder;
        }

        void HandleMovementInput()
        {
            if (isCrouching)
            {
                movementDirection = Vector2.zero;
                return;
            }

            Vector2 input = Vector2.zero;

            if (Input.GetKey(KeyCode.W))
            {
                input.y += 1f;
            }

            if (Input.GetKey(KeyCode.S))
            {
                input.y -= 1f;
            }

            if (Input.GetKey(KeyCode.D))
            {
                input.x += 1f;
            }

            if (Input.GetKey(KeyCode.A))
            {
                input.x -= 1f;
            }

            if (input == Vector2.zero)
            {
                movementDirection = Vector2.zero;
                return;
            }

            input.Normalize();
            float rawAngle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;
            lastAngle = SnapAngleToEightDirections(rawAngle);
            movementDirection = AngleToVector(lastAngle);
        }

        Vector2 AngleToVector(float angle)
        {
            float radians = angle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        }

        float SnapAngleToEightDirections(float angle)
        {
            angle = (angle + 360f) % 360f;

            if (isOnStairs)
            {
                if (angle < 30f || angle >= 330f) return 0f;
                if (angle < 75f) return 60f;
                if (angle < 105f) return 90f;
                if (angle < 150f) return 120f;
                if (angle < 210f) return 180f;
                if (angle < 255f) return 240f;
                if (angle < 285f) return 270f;
                return 300f;
            }

            if (angle < 15f || angle >= 345f) return 0f;
            if (angle < 60f) return 35f;
            if (angle < 120f) return 90f;
            if (angle < 165f) return 145f;
            if (angle < 195f) return 180f;
            if (angle < 240f) return 215f;
            if (angle < 300f) return 270f;
            return 330f;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Stairs"))
            {
                isOnStairs = true;
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Stairs"))
            {
                isOnStairs = false;
            }
        }

        void HandleCrouching()
        {
            isCrouching = !isCrouching;
            speed = isCrouching ? 1.0f : 2.0f;

            if (isCrouching && isStealth)
            {
                spriteRenderer.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }
            else
            {
                spriteRenderer.color = Color.white;
            }
        }

        public void SetArcherStatus(bool status)
        {
            isRanged = status;
        }

        void DelayedShoot()
        {
            if (projectilePrefab == null)
            {
                return;
            }

            Vector2 fireDirection = AngleToVector(lastAngle);
            ShootProjectile(fireDirection);
        }

        void ShootProjectile(Vector2 direction)
        {
            if (projectilePrefab == null)
            {
                return;
            }

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            GameObject projectileInstance = Instantiate(projectilePrefab, transform.position, Quaternion.Euler(0f, 0f, angle));

            if (projectileInstance.TryGetComponent<Rigidbody2D>(out var rbProjectile))
            {
                rbProjectile.velocity = direction * projectileSpeed;
            }
        }

        IEnumerator Quickshot()
        {
            if (projectilePrefab == null)
            {
                yield break;
            }

            yield return new WaitForSeconds(0.1f);

            for (int i = 0; i < 5; i++)
            {
                ShootProjectile(AngleToVector(lastAngle));
                yield return new WaitForSeconds(0.18f);
            }
        }

        IEnumerator CircleShot()
        {
            if (projectilePrefab == null)
            {
                yield break;
            }

            yield return new WaitForSeconds(0.1f);
            float timeBetweenShots = 0.9f / 8f;

            for (int i = 0; i < 8; i++)
            {
                float angle = lastAngle + i * 45f;
                ShootProjectile(AngleToVector(angle));
                yield return new WaitForSeconds(timeBetweenShots);
            }
        }

        IEnumerator DeployAoEDelayed()
        {
            if (AoEPrefab == null)
            {
                yield break;
            }

            yield return new WaitForSeconds(0.3f);
            GameObject aoeInstance = Instantiate(AoEPrefab, transform.position, Quaternion.identity);
            Destroy(aoeInstance, 0.5f);
        }
    }
}
