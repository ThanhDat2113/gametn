using UnityEngine;

namespace OccaSoftware.MotionBlur.Demo
{
    public class MBRotate : MonoBehaviour
    {
        [SerializeField]
        float speed;

        [SerializeField]
        float frequency = 1f;

        void Update()
        {
            transform.Rotate(
                Vector3.one,
                speed * Time.deltaTime * Mathf.Sin(Time.time * frequency)
            );
        }
    }
}
