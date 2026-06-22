using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace OccaSoftware.MotionBlur.Demo
{
	public class VehicleController : MonoBehaviour
	{
		[SerializeField]
		Rigidbody rb;

		float maxAccel = 10f;
		Vector3 velocity;

		float targetVelocity;

		float maxSpeed = 20f;

		float rot = 0f;

		void Update()
		{
			float rotation = Input.GetAxis("Mouse X");
			rot = Mathf.MoveTowards(rot, rotation, 10f * Time.deltaTime);
			transform.Rotate(0, rot * Time.deltaTime * 180f, 0);
			targetVelocity = Input.GetAxisRaw("Vertical") * maxSpeed;
		}

		private void FixedUpdate()
		{
#if UNITY_2023_3_OR_NEWER
			velocity = rb.linearVelocity;
#else
            velocity = rb.velocity;
#endif
            float maxDelta = maxAccel * Time.deltaTime;
			velocity.z = Mathf.MoveTowards(velocity.z, targetVelocity, maxDelta);

#if UNITY_2023_3_OR_NEWER
			rb.linearVelocity = velocity;
#else
            rb.velocity = velocity;
#endif
        }
	}
}

