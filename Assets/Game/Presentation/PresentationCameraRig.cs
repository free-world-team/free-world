using UnityEngine;

namespace Game.Presentation
{
    /// <summary>Tilted orthographic follower for the X/Z 2.5D presentation plane.</summary>
    public sealed class PresentationCameraRig : MonoBehaviour
    {
        private static readonly Vector3 CameraOffset = new Vector3(0f, 12.5f, -11.5f);
        private static readonly Vector3 FocusOffset = new Vector3(0f, 0.72f, 2.1f);
        private Transform target;
        private Rect bounds = new Rect(-1000f, -1000f, 2000f, 2000f);
        private float shakeAmplitude;
        private float shakeRemaining;
        private float shakeDuration;
        private Vector2 motionLead;
        private Vector3 lastTargetPosition;
        private bool hasTargetSample;

        public bool EffectsEnabled { get; set; } = true;
        public bool UsesTiltedOrthographicProjection { get; private set; }
        public Vector3 LastStablePosition { get; private set; }
        public Vector2 MotionLead => motionLead;

        public void ConfigureTiltedOrthographic(Camera presentationCamera)
        {
            if (presentationCamera == null) return;
            presentationCamera.orthographic = true;
            presentationCamera.orthographicSize = 8.6f;
            presentationCamera.nearClipPlane = 0.1f;
            presentationCamera.farClipPlane = 80f;
            presentationCamera.clearFlags = CameraClearFlags.SolidColor;
            presentationCamera.backgroundColor = new Color(0.025f, 0.045f, 0.055f, 1f);
            presentationCamera.transparencySortMode = TransparencySortMode.CustomAxis;
            presentationCamera.transparencySortAxis = new Vector3(0f, 0f, 1f);
            UsesTiltedOrthographicProjection = true;
            SetStablePose(Vector3.zero);
        }

        public void SetTarget(Transform value)
        {
            if (target == value) return;
            target = value;
            motionLead = Vector2.zero;
            hasTargetSample = value != null;
            lastTargetPosition = value == null ? Vector3.zero : value.position;
        }

        public void SetBounds(Rect value)
        {
            if (value.width <= 0f || value.height <= 0f) return;
            bounds = value;
        }

        public void RequestShake(float amplitude, float duration)
        {
            if (!EffectsEnabled || amplitude <= 0f || duration <= 0f) return;
            shakeAmplitude = Mathf.Max(shakeAmplitude, amplitude);
            shakeRemaining = Mathf.Max(shakeRemaining, duration);
            shakeDuration = Mathf.Max(shakeDuration, duration);
        }

        public void TickCamera(float unscaledDeltaTime)
        {
            var focus = Vector3.zero;
            if (target != null)
            {
                var targetPosition = target.position;
                var desiredLead = Vector2.zero;
                if (hasTargetSample)
                {
                    var delta = new Vector2(
                        targetPosition.x - lastTargetPosition.x,
                        targetPosition.z - lastTargetPosition.z);
                    if (delta.sqrMagnitude > 0.0001f) desiredLead = delta.normalized * 1.2f;
                }
                motionLead = Vector2.MoveTowards(
                    motionLead,
                    desiredLead,
                    Mathf.Max(0f, unscaledDeltaTime) * 4.8f);
                focus.x = Mathf.Clamp(targetPosition.x + motionLead.x, bounds.xMin, bounds.xMax);
                focus.z = Mathf.Clamp(targetPosition.z + motionLead.y, bounds.yMin, bounds.yMax);
                lastTargetPosition = targetPosition;
                hasTargetSample = true;
            }
            else
            {
                focus.x = LastStablePosition.x - CameraOffset.x - FocusOffset.x;
                focus.z = LastStablePosition.z - CameraOffset.z - FocusOffset.z;
            }

            SetStablePose(focus);
            var current = LastStablePosition;
            if (EffectsEnabled && shakeRemaining > 0f)
            {
                shakeRemaining = Mathf.Max(0f, shakeRemaining - unscaledDeltaTime);
                var normalized = shakeDuration > 0f ? shakeRemaining / shakeDuration : 0f;
                var phase = Time.unscaledTime * 47f;
                current += transform.right * (Mathf.Sin(phase) * shakeAmplitude * normalized);
                current += transform.up * (Mathf.Cos(phase * 1.37f) * shakeAmplitude * normalized);
            }
            transform.position = current;
        }

        private void SetStablePose(Vector3 simulationFocus)
        {
            var focus = new Vector3(simulationFocus.x, 0f, simulationFocus.z) + FocusOffset;
            LastStablePosition = focus + CameraOffset;
            transform.SetPositionAndRotation(
                LastStablePosition,
                Quaternion.LookRotation(focus - LastStablePosition, Vector3.up));
        }

        private void LateUpdate() => TickCamera(Time.unscaledDeltaTime);
    }
}
