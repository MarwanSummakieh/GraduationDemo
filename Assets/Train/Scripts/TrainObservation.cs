using System.Collections.Generic;
using UnityEngine;

namespace GraduationDemo.Train
{
    public sealed class TrainObservation : MonoBehaviour
    {
        public static readonly HashSet<TrainObservation> Active = new HashSet<TrainObservation>();
        public string title;
        [Tooltip("Stable notebook identity. Leave empty to use the object's hierarchy path.")]
        public string memoryId;
        [TextArea] public string description;
        public TrainActor speaker;
        public float interactionRange = 2.3f;
        public string[] topics;
        [TextArea] public string[] responses;
        private void OnEnable() { Active.Add(this); }
        private void OnDisable() { Active.Remove(this); }
    }
}
