using UnityEngine;

namespace OccaSoftware.MotionBlur.Demo
{
    public class MBSlide : MonoBehaviour
    {
        [SerializeField]
        float speed = 1f;

        [SerializeField]
        float distance = 1f;

        private Vector3 start;
        private Vector3 end;

        void Start()
        {
            start = transform.position - Vector3.right * distance;
            end = transform.position + Vector3.right * distance;
        }

        void Update()
        {
            float t = (Mathf.Sin(Time.realtimeSinceStartup * speed) + 1.0f) * 0.5f;
            transform.position = Vector3.Lerp(start, end, t);
        }
    }
}
