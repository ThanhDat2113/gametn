using UnityEngine;

namespace OccaSoftware.MotionBlur.Demo
{
    public class MBCameraRotateBetween : MonoBehaviour
    {
        [SerializeField]
        private Vector3 rotationA;

        [SerializeField]
        private Vector3 rotationB;

        [SerializeField]
        private float frequency;

        // Start is called before the first frame update
        void Start()
        {
            transform.rotation = Quaternion.Euler(rotationA);
        }

        // Update is called once per frame
        void Update()
        {
            transform.rotation = Quaternion.Slerp(
                Quaternion.Euler(rotationA),
                Quaternion.Euler(rotationB),
                Mathf.Abs(((Time.time * frequency) % 2f) - 1f)
            );
        }
    }
}
