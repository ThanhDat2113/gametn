using UnityEngine;

namespace OccaSoftware.MotionBlur.Demo
{
    public class Rotate : MonoBehaviour
    {
        public Vector3 speed = new Vector3(0, 0, 360f);

        void Update()
        {
            transform.Rotate(speed * Time.deltaTime, Space.World);
        }
    }
}
