using UnityEngine;

namespace GraduationDemo.Train
{
    /// <summary>Uses the collection's existing body and facial animation controllers.</summary>
    public sealed class TrainActor : MonoBehaviour
    {
        public Animator bodyAnimator;
        public Animator facialAnimator;
        public CharacterController locomotion;
        public Transform detective;
        public bool secondaryIdle;
        public string role;
        public string characterId;
        public bool seated;
        public float seatHeight = .60f;
        private Quaternion restingRotation;
        private Vector3 previousPosition;
        private float blinkAt;
        private bool talking;
        private static readonly int Walk = Animator.StringToHash("Walk 1");
        private static readonly int AlternateIdle = Animator.StringToHash("Idle 2");
        private static readonly int Talk = Animator.StringToHash("Talk");
        private static readonly int Blink = Animator.StringToHash("Blink");

        private void Awake()
        {
            restingRotation = transform.rotation;
            bodyAnimator.applyRootMotion = false;
            bodyAnimator.SetBool(AlternateIdle, secondaryIdle);
            if (facialAnimator != null) facialAnimator.applyRootMotion = false;
        }
        private void OnEnable()
        {
            previousPosition = transform.position;
            blinkAt = Time.time + Random.Range(2.5f, 5.5f);
        }
        private void LateUpdate()
        {
            if (locomotion != null)
            {
                Vector3 movement = transform.position - previousPosition; movement.y = 0;
                bodyAnimator.SetBool(Walk, movement.sqrMagnitude > .00001f && movement.sqrMagnitude < 1f);
                previousPosition = transform.position;
            }
            else if (!seated && detective != null)
            {
                Vector3 towardPlayer = detective.position - transform.position; towardPlayer.y = 0;
                bool engaged = detective.gameObject.activeInHierarchy && (talking || towardPlayer.sqrMagnitude < 6.25f);
                Quaternion target = engaged && towardPlayer.sqrMagnitude > .01f ? Quaternion.LookRotation(towardPlayer) : restingRotation;
                transform.rotation = Quaternion.Slerp(transform.rotation, target, 1 - Mathf.Exp(-Time.deltaTime * 4));
            }
            if (facialAnimator != null && Time.time >= blinkAt)
            {
                facialAnimator.SetTrigger(Blink);
                blinkAt = Time.time + Random.Range(3, 6);
            }
            if (seated) ApplySeatedPose();
        }

        public void ApplySeatedPose()
        {
            if (!seated || bodyAnimator == null || !bodyAnimator.isHuman) return;
            var hips = bodyAnimator.GetBoneTransform(HumanBodyBones.Hips);
            hips.position = transform.position + Vector3.up * seatHeight;
            Vector3 forward = transform.forward;
            SolveLimb(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot,
                transform.position - transform.right * .11f + forward * .43f + Vector3.up * .10f, forward);
            SolveLimb(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot,
                transform.position + transform.right * .11f + forward * .43f + Vector3.up * .10f, forward);
            AimFoot(HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes, forward);
            AimFoot(HumanBodyBones.RightFoot, HumanBodyBones.RightToes, forward);
            SolveLimb(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand,
                hips.position - transform.right * .19f + forward * .23f + Vector3.up * .08f, -transform.right);
            SolveLimb(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand,
                hips.position + transform.right * .19f + forward * .23f + Vector3.up * .08f, transform.right);
            if (detective != null && detective.gameObject.activeInHierarchy)
            {
                Vector3 toward = detective.position - transform.position; toward.y = 0;
                if (toward.sqrMagnitude < 12 && toward.sqrMagnitude > .01f)
                {
                    float yaw = Mathf.Clamp(Vector3.SignedAngle(forward, toward, Vector3.up), -40, 40);
                    var head = bodyAnimator.GetBoneTransform(HumanBodyBones.Head);
                    head.rotation = Quaternion.AngleAxis(yaw * .65f, Vector3.up) * head.rotation;
                }
            }
        }

        private void SolveLimb(HumanBodyBones upper, HumanBodyBones lower, HumanBodyBones end, Vector3 target, Vector3 bend)
        {
            Transform a = bodyAnimator.GetBoneTransform(upper), b = bodyAnimator.GetBoneTransform(lower), c = bodyAnimator.GetBoneTransform(end);
            if (a == null || b == null || c == null) return;
            float first = Vector3.Distance(a.position, b.position), second = Vector3.Distance(b.position, c.position);
            Vector3 delta = target - a.position;
            float distance = Mathf.Clamp(delta.magnitude, Mathf.Abs(first - second) + .001f, first + second - .001f);
            Vector3 axis = delta.normalized;
            float along = (first * first - second * second + distance * distance) / (2 * distance);
            Vector3 outward = Vector3.ProjectOnPlane(bend, axis).normalized;
            Vector3 joint = a.position + axis * along + outward * Mathf.Sqrt(Mathf.Max(0, first * first - along * along));
            a.rotation = Quaternion.FromToRotation(b.position - a.position, joint - a.position) * a.rotation;
            b.rotation = Quaternion.FromToRotation(c.position - b.position, target - b.position) * b.rotation;
        }

        private void AimFoot(HumanBodyBones footBone, HumanBodyBones toesBone, Vector3 forward)
        {
            var foot = bodyAnimator.GetBoneTransform(footBone); var toes = bodyAnimator.GetBoneTransform(toesBone);
            if (foot != null && toes != null) foot.rotation = Quaternion.FromToRotation(toes.position - foot.position, forward) * foot.rotation;
        }
        public void SetTalking(bool value)
        {
            talking = value;
            if (facialAnimator != null) facialAnimator.SetBool(Talk, value);
        }
        private void OnDisable()
        {
            if (bodyAnimator != null) bodyAnimator.SetBool(Walk, false);
            SetTalking(false);
        }
    }
}
