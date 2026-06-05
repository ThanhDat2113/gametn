using System.Collections.Generic;

using UnityEngine;

namespace OccaSoftware.MotionBlur.Demo
{
    public class Spawner : MonoBehaviour
    {
        [SerializeField]
        private List<GameObject> gameObjects = new List<GameObject>();

        [SerializeField]
        private Transform target;

        [SerializeField]
        private int gap = 15;

        [SerializeField]
        private int targetCount = 20;

        private Queue<GameObject> activeGameObjects = new Queue<GameObject>();
        private float positionTracker;
        private float maxDistance;

        void Start()
        {
            positionTracker = target.transform.position.z - gap * 2;
        }

        void Update()
        {
            UpdateQueue();
        }

        void UpdateQueue()
        {
            maxDistance = target.transform.position.z + gap * (targetCount - 2);

            while (positionTracker < maxDistance)
            {
                int r = Random.Range(0, gameObjects.Count - 1);
                GameObject go = Instantiate(
                    gameObjects[r],
                    new Vector3(transform.position.x, transform.position.y, positionTracker),
                    Quaternion.identity
                );
                activeGameObjects.Enqueue(go);
                positionTracker += gap;
            }

            while (activeGameObjects.Count > targetCount)
            {
                if (activeGameObjects.TryDequeue(out GameObject go))
                {
                    Destroy(go);
                }
            }
        }
    }
}
