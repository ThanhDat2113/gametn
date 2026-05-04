using UnityEngine;

namespace OccaSoftware.MotionBlur.Demo
{
    public class TranslateObject : MonoBehaviour
    {
        [SerializeField]
        float x;

        [SerializeField]
        float y;

        [SerializeField]
        float rate = 5f;
        Vector3 sourcePosition;

        private void Start()
        {
            sourcePosition = transform.position;
        }

        void Update()
        {
            float d = Mathf.Sin(Time.time * rate);
            transform.position = sourcePosition + new Vector3(x * d, y * d, 0);
        }
    }
}
