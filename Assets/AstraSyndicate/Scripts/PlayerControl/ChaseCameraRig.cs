using AstraSyndicate.Combat;
using UnityEngine;

namespace AstraSyndicate.PlayerControl
{
    /// <summary>
    /// Third-person camera behind the flagship, looking along the player's aim. Adds a speed-based FOV kick when
    /// boosting and a short shake when the flagship is hit.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class ChaseCameraRig : MonoBehaviour
    {
        [SerializeField] private PlayerFlightController target;
        [SerializeField, Min(1f)] private float distance = 22f;
        [SerializeField] private float height = 5f;
        [SerializeField, Min(0.1f)] private float positionSharpness = 10f;
        [SerializeField, Min(0.1f)] private float rotationSharpness = 12f;
        [Tooltip("Extra field of view (degrees) while boosting.")]
        [SerializeField, Range(0f, 30f)] private float boostFovKick = 10f;
        [Tooltip("Shake added per point of hull damage taken.")]
        [SerializeField, Min(0f)] private float shakePerDamage = 0.04f;
        [SerializeField, Min(0f)] private float maxShake = 0.6f;

        private Camera _camera;
        private Ship _subscribedShip;
        private float _baseFov;
        private float _shake;

        public void SetTarget(PlayerFlightController flight)
        {
            target = flight;
            if (_camera != null) SnapToTarget();
        }

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _baseFov = _camera.fieldOfView;
        }

        private void Start()
        {
            if (target != null) SnapToTarget();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                Unsubscribe();
                return;
            }
            if (_subscribedShip != target.Ship) Subscribe(target.Ship);

            float deltaTime = Time.deltaTime;
            Quaternion aim = target.AimRotation;
            Vector3 desiredPosition = target.transform.position - aim * Vector3.forward * distance + aim * Vector3.up * height;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, 1f - Mathf.Exp(-positionSharpness * deltaTime));
            transform.rotation = Quaternion.Slerp(transform.rotation, aim, 1f - Mathf.Exp(-rotationSharpness * deltaTime));

            float fov = _baseFov + (target.Motor.IsBoosting ? boostFovKick : 0f);
            _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, fov, 1f - Mathf.Exp(-4f * deltaTime));

            if (_shake > 0f)
            {
                transform.position += Random.insideUnitSphere * _shake;
                _shake = Mathf.Max(0f, _shake - deltaTime * 1.5f);
            }
        }

        private void SnapToTarget()
        {
            Quaternion aim = target.transform.rotation;
            transform.SetPositionAndRotation(target.transform.position - aim * Vector3.forward * distance + aim * Vector3.up * height, aim);
        }

        private void Subscribe(Ship ship)
        {
            Unsubscribe();
            _subscribedShip = ship;
            if (_subscribedShip != null) _subscribedShip.Damaged += OnDamaged;
        }

        private void Unsubscribe()
        {
            if (_subscribedShip != null) _subscribedShip.Damaged -= OnDamaged;
            _subscribedShip = null;
        }

        private void OnDamaged(Ship ship, DamageInfo damage)
        {
            _shake = Mathf.Min(maxShake, _shake + damage.Amount * shakePerDamage);
        }
    }
}
