using System.Collections.Generic;
using UnityEngine;

namespace SmallScaleInc.TopDownPixelCharactersPack1.Animation
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class EnemyAnimationBase : MonoBehaviour
    {
        private const float VelocityThreshold = 0.05f;
        private const float MovementDotThreshold = 0.45f;

        [SerializeField] private string defaultDirectionBool = "isEast";
        [SerializeField] private string walkBool = "isWalking";
        [SerializeField] private string runBool = "isRunning";

        private readonly Dictionary<string, AnimatorControllerParameterType> _parameterLookup = new();

        protected Animator Animator { get; private set; }
        protected Rigidbody2D Body { get; private set; }

        protected string CurrentDirection { get; private set; } = "isEast";
        protected string CurrentDirectionSuffix => CurrentDirection.Length > 2 ? CurrentDirection.Substring(2) : string.Empty;

        private Vector2 _lastMoveVector = Vector2.right;

        protected virtual void Awake()
        {
            Animator = GetComponent<Animator>();
            Body = GetComponent<Rigidbody2D>();

            CacheAnimatorParameters();
        }

        protected virtual void Start()
        {
            if (!string.IsNullOrEmpty(defaultDirectionBool))
            {
                TrySetBool(defaultDirectionBool, true);
                CurrentDirection = defaultDirectionBool;
            }
        }

        protected virtual void Update()
        {
            HandleMovement();
        }

        private void HandleMovement()
        {
            Vector2 velocity = Body.velocity;
            bool hasMovement = velocity.sqrMagnitude > VelocityThreshold * VelocityThreshold;
            Vector2 moveVector = hasMovement ? velocity.normalized : _lastMoveVector;

            if (hasMovement)
            {
                _lastMoveVector = moveVector;
            }

            float facingAngle = Mathf.Atan2(moveVector.y, moveVector.x) * Mathf.Rad2Deg;
            string newDirection = DetermineDirectionFromAngle(facingAngle);
            UpdateDirection(newDirection);

            string movementDirection = newDirection.Substring(2);

            Vector2 facingVector = new Vector2(Mathf.Cos(facingAngle * Mathf.Deg2Rad), Mathf.Sin(facingAngle * Mathf.Deg2Rad));
            Vector2 rightVector = new Vector2(-facingVector.y, facingVector.x);

            float forwardDot = hasMovement ? Vector2.Dot(moveVector, facingVector) : 0f;
            float rightDot = hasMovement ? Vector2.Dot(moveVector, rightVector) : 0f;

            bool isRunning = hasMovement && forwardDot > MovementDotThreshold;
            bool isRunningBackwards = hasMovement && forwardDot < -MovementDotThreshold;
            bool isStrafingLeft = hasMovement && rightDot < -MovementDotThreshold;
            bool isStrafingRight = hasMovement && rightDot > MovementDotThreshold;

            TrySetBool(walkBool, hasMovement);
            TrySetBool(runBool, isRunning);
            TrySetBool("isRunning", isRunning);
            TrySetBool("isRunningBackwards", isRunningBackwards);
            TrySetBool("isStrafingLeft", isStrafingLeft);
            TrySetBool("isStrafingRight", isStrafingRight);
            TrySetBool("isCrouchRunning", false);
            TrySetBool("isCrouchIdling", false);

            ResetAllMovementBools();

            SetMovementAnimation(isRunning, "Move", movementDirection);
            SetMovementAnimation(isRunningBackwards, "RunBackwards", movementDirection);
            SetMovementAnimation(isStrafingLeft, "StrafeLeft", movementDirection);
            SetMovementAnimation(isStrafingRight, "StrafeRight", movementDirection);

            if (hasMovement && !isRunning && !isRunningBackwards && !isStrafingLeft && !isStrafingRight)
            {
                SetMovementAnimation(true, "Move", movementDirection);
            }

            OnAfterMovementEvaluated(hasMovement, movementDirection, forwardDot, rightDot);
        }

        protected virtual void OnAfterMovementEvaluated(bool hasMovement, string movementDirection, float forwardDot, float rightDot)
        {
        }

        protected void TriggerIfExists(string triggerName)
        {
            if (string.IsNullOrEmpty(triggerName))
            {
                return;
            }

            if (HasParameter(triggerName, AnimatorControllerParameterType.Trigger))
            {
                Animator.SetTrigger(triggerName);
            }
        }

        protected void UpdateDirection(string newDirection)
        {
            if (CurrentDirection == newDirection)
            {
                return;
            }

            SetDirectionBools(false, false, false, false, false, false, false, false);
            TrySetBool(newDirection, true);
            CurrentDirection = newDirection;
        }

        private void SetMovementAnimation(bool isActive, string baseKey, string direction)
        {
            if (!isActive)
            {
                return;
            }

            string parameterName = $"{baseKey}{direction}";
            TrySetBool(parameterName, true);
        }

        private void ResetAllMovementBools()
        {
            string[] directions = { "North", "South", "East", "West", "NorthEast", "NorthWest", "SouthEast", "SouthWest" };
            string[] baseKeys = { "Move", "RunBackwards", "StrafeLeft", "StrafeRight", "CrouchRun" };

            foreach (string baseKey in baseKeys)
            {
                foreach (string direction in directions)
                {
                    TrySetBool($"{baseKey}{direction}", false);
                }
            }
        }

        protected string DetermineDirectionFromAngle(float angle)
        {
            angle = (angle + 360f) % 360f;

            if (angle < 15f || angle >= 345f) return "isEast";
            if (angle < 60f) return "isNorthEast";
            if (angle < 120f) return "isNorth";
            if (angle < 165f) return "isNorthWest";
            if (angle < 195f) return "isWest";
            if (angle < 240f) return "isSouthWest";
            if (angle < 300f) return "isSouth";
            return "isSouthEast";
        }

        private void SetDirectionBools(bool isWest, bool isEast, bool isSouth, bool isSouthWest, bool isNorthEast, bool isSouthEast, bool isNorth, bool isNorthWest)
        {
            TrySetBool("isWest", isWest);
            TrySetBool("isEast", isEast);
            TrySetBool("isSouth", isSouth);
            TrySetBool("isSouthWest", isSouthWest);
            TrySetBool("isNorthEast", isNorthEast);
            TrySetBool("isSouthEast", isSouthEast);
            TrySetBool("isNorth", isNorth);
            TrySetBool("isNorthWest", isNorthWest);
        }

        protected void FaceDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            string newDirection = DetermineDirectionFromAngle(angle);
            UpdateDirection(newDirection);
        }

        private void CacheAnimatorParameters()
        {
            _parameterLookup.Clear();
            if (Animator == null)
            {
                return;
            }

            foreach (var parameter in Animator.parameters)
            {
                if (!_parameterLookup.ContainsKey(parameter.name))
                {
                    _parameterLookup.Add(parameter.name, parameter.type);
                }
            }
        }

        protected bool TrySetBool(string parameterName, bool value)
        {
            if (string.IsNullOrEmpty(parameterName) || !HasParameter(parameterName, AnimatorControllerParameterType.Bool))
            {
                return false;
            }

            Animator.SetBool(parameterName, value);
            return true;
        }

        protected bool HasParameter(string parameterName, AnimatorControllerParameterType type)
        {
            return _parameterLookup.TryGetValue(parameterName, out var storedType) && storedType == type;
        }
    }
}
