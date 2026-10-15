using UnityEngine;
using UnityEngine.InputSystem;

namespace GraduationDemo.Train
{
    /// <summary>Two stationary perspective cameras per carriage and one shared detective camera.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class TrainCameraRig : MonoBehaviour
    {
        public TrainTour tour;
        public Camera[] leftCorners;
        public Camera[] rightCorners;
        public Camera detectiveCamera;
        [Min(0)] public float sideDeadZone = .55f;
        [Min(0)] public float carriageDeadZone = .25f;
        public float followDistance = 3.2f;
        public float followHeight = 1.45f;
        public float mouseSensitivity = .15f;
        public LayerMask obstructionLayers = ~0;
        public bool Following { get; private set; }
        public Camera ActiveCamera { get; private set; }
        public int CurrentCarriage { get; private set; } = -1;
        public bool UsingRightCorner { get; private set; }
        float yaw = 90, pitch = 22;
        bool moving;
        Vector3 movementForward, movementRight;

        void Start() { tour.cameraRig = this; Refresh(true); }

        void LateUpdate()
        {
            if (Following && (tour.narrative == null || !tour.narrative.Busy) && string.IsNullOrEmpty(tour.CurrentResponse))
            {
                var mouse = Mouse.current;
                if (mouse != null && mouse.rightButton.isPressed)
                {
                    Vector2 delta = mouse.delta.ReadValue();
                    yaw += delta.x * mouseSensitivity;
                    pitch = Mathf.Clamp(pitch - delta.y * mouseSensitivity, 10, 65);
                }
            }
            Refresh(false);
        }

        public void ToggleFollow()
        {
            Following = !Following;
            if (Following) yaw = tour.detective.transform.eulerAngles.y;
            ResetMovement();
            Refresh(true);
        }

        public void ResetMovement() { moving = false; }

        public Vector3 Movement(Vector2 input)
        {
            if (input.sqrMagnitude < .01f) { ResetMovement(); return Vector3.zero; }
            if (ActiveCamera == null) Refresh(true);
            // Preserve the input basis through fixed-camera cuts until the player releases movement.
            if (!moving || Following)
            {
                movementForward = Vector3.ProjectOnPlane(ActiveCamera.transform.forward, Vector3.up).normalized;
                movementRight = Vector3.Cross(Vector3.up, movementForward);
                moving = true;
            }
            return Vector3.ClampMagnitude(movementRight * input.x + movementForward * input.y, 1);
        }

        public void Refresh(bool snap)
        {
            if (tour == null || tour.detective == null || tour.roomViews.Length == 0) return;
            float x = tour.detective.transform.position.x;
            int nearest = 0;
            for (int i = 1; i < tour.roomViews.Length; i++)
                if (Mathf.Abs(x - tour.roomViews[i].position.x) < Mathf.Abs(x - tour.roomViews[nearest].position.x)) nearest = i;
            int room = CurrentCarriage;
            if (snap || room < 0 || Mathf.Abs(x - tour.roomViews[nearest].position.x) + carriageDeadZone * 2 < Mathf.Abs(x - tour.roomViews[room].position.x)) room = nearest;
            float localX = x - tour.roomViews[room].position.x;
            if (snap || room != CurrentCarriage) UsingRightCorner = localX <= 0;
            else if (localX < -sideDeadZone) UsingRightCorner = true;
            else if (localX > sideDeadZone) UsingRightCorner = false;
            CurrentCarriage = room;
            tour.currentRoom = room;
            if (Following) PositionFollowCamera(snap);
            var next = Following ? detectiveCamera : UsingRightCorner ? rightCorners[room] : leftCorners[room];
            foreach (var camera in leftCorners) Activate(camera, camera == next);
            foreach (var camera in rightCorners) Activate(camera, camera == next);
            Activate(detectiveCamera, detectiveCamera == next);
            ActiveCamera = next;
            tour.viewCamera = next;
        }

        static void Activate(Camera camera, bool active)
        {
            camera.enabled = active;
            camera.tag = active ? "MainCamera" : "Untagged";
            var listener = camera.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = active;
        }

        void PositionFollowCamera(bool snap)
        {
            Vector3 pivot = tour.detective.transform.position + Vector3.up * followHeight;
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
            Vector3 offset = rotation * Vector3.back * followDistance;
            float distance = followDistance;
            foreach (var hit in Physics.SphereCastAll(pivot, .18f, offset.normalized, followDistance, obstructionLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.transform.IsChildOf(tour.detective.transform)) continue;
                distance = Mathf.Min(distance, Mathf.Max(.1f, hit.distance - .08f));
            }
            Vector3 desired = pivot + offset.normalized * distance;
            // Resolve collision immediately; smooth only unobstructed movement.
            float blend = snap || distance < followDistance ? 1 : 1 - Mathf.Exp(-Time.deltaTime * 12);
            Vector3 position = Vector3.Lerp(detectiveCamera.transform.position, desired, blend);
            // Keep the smoothed position on the tested boom, so corners cannot be crossed by interpolation.
            position = pivot + offset.normalized * Mathf.Min(distance, Vector3.Distance(position, pivot));
            detectiveCamera.transform.SetPositionAndRotation(position, rotation);
        }
    }
}
