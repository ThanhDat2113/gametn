using UnityEngine;

namespace OccaSoftware.MotionBlur.Demo
{
    public class MoveForward : MonoBehaviour
    {
        [SerializeField]
        Camera cam;

        [SerializeField]
        float maxSpeed = 60f;

        float currentSpeed
        {
            get => currentSpeed01 * maxSpeed;
        }
        float currentSpeed01;

        [SerializeField]
        UnityEngine.UI.Slider slider;

        void Start()
        {
            currentSpeed01 = 0.0f;
        }

        void Update()
        {
            currentSpeed01 = slider.value;
            transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime, Space.World);
            /* Note: Adjusting the field of view causes sudden jumps in the motion vector texture. Avoid it.
             *
             * cam.fieldOfView = Mathf.Lerp(50, 70, currentSpeed01);
            */
        }
    }
}
