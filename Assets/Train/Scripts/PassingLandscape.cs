using UnityEngine;

namespace GraduationDemo.Train
{
    public sealed class PassingLandscape : MonoBehaviour
    {
        public float speed = 5;
        public float minimumX = -30;
        public float maximumX = 72;
        private void Update()
        {
            foreach (Transform child in transform)
            {
                child.position += Vector3.left * (speed * Time.deltaTime);
                if (child.position.x < minimumX)
                    child.position += Vector3.right * (maximumX - minimumX);
            }
        }
    }
}
