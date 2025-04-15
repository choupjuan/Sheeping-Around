using UnityEngine;

namespace Ursaanimation.CubicFarmAnimals
{
    public class AnimationController : MonoBehaviour
    {
        public Animator animator;
        public string walkForwardAnimation = "walk_forward";
        public string walkBackwardAnimation = "walk_backwards";
        public string runForwardAnimation = "run_forward";
        public string turn90LAnimation = "turn_90_L";
        public string turn90RAnimation = "turn_90_R";
        public string trotAnimation = "trot_forward";
        public string sittostandAnimation = "sit_to_stand";
        public string standtositAnimation = "stand_to_sit";

        public Vector3 playAreaCenter = Vector3.zero; // Center of the play area

        public Vector3 playAreaSize = new Vector3(40f, 40f, 40f); // Size of the play area
        
        public float randomWalkInterval = 2f; // Time in seconds between random actions
        private float timeSinceLastAction = 0f;

        public float moveSpeed = 2f; // Movement speed for forward/backward
        public float turnSpeed = 90f; // Turn speed in degrees per second
        public float raycastDistance = 1.5f; // Distance to detect obstacles
        public float avoidTurnAngle = 30f; // Angle to turn to avoid obstacles

        private bool isMoving = false; // Track if the sheep is currently moving forward
        private bool isTurning = false; // Track if the sheep is currently turning

        void Start()
        {
            animator = GetComponent<Animator>();
        }

        void Update()
        {
            if (!isTurning) // Only allow forward movement if not turning
            {
                if (isMoving)
                {
                    MoveForwardWithAvoidance();
                }

                RandomWalk();
            }
        }

        private void RandomWalk()
        {
            // Timer for random actions
            timeSinceLastAction += Time.deltaTime;
            if (timeSinceLastAction >= randomWalkInterval)
            {
                PerformRandomAction();
                timeSinceLastAction = 0f; // Reset the timer
            }
        }

        private void PerformRandomAction()
        {
            // Choose a random action
            int randomAction = Random.Range(0, 4); // Random number from 0 to 4

            switch (randomAction)
            {
                case 0: // Walk forward
                    animator.Play(walkForwardAnimation);
                    isMoving = true;
                    break;
                case 1: // Turn left
                    StartCoroutine(Turn(-90f));
                    break;
                case 2: // Turn right
                    StartCoroutine(Turn(90f));
                    break;
                case 3: // Trot forward
                    animator.Play(trotAnimation);
                    isMoving = true;
                    break;
                default:
                    // Optional: Add idle or no action
                    break;
            }
        }
        private void KeepWithinPlayArea()
        {
            // Calculate the boundaries of the play area
            float minX = playAreaCenter.x - playAreaSize.x / 2;
            float maxX = playAreaCenter.x + playAreaSize.x / 2;
            float minZ = playAreaCenter.z - playAreaSize.z / 2;
            float maxZ = playAreaCenter.z + playAreaSize.z / 2;

            // Clamp the sheep's position within the boundaries
            Vector3 position = transform.position;
            position.x = Mathf.Clamp(position.x, minX, maxX);
            position.z = Mathf.Clamp(position.z, minZ, maxZ);
            transform.position = position;

            // Optionally, make the sheep turn back if it hits the boundary
            if (position.x == minX || position.x == maxX || position.z == minZ || position.z == maxZ)
            {
                StartCoroutine(Turn(Random.Range(90f, 180f))); // Turn around randomly between 90 and 180 degrees
            }
        }

        private void MoveForwardWithAvoidance()
        {
            KeepWithinPlayArea(); // Keep the sheep within the play area
            // Raycast in front to detect obstacles
            RaycastHit hit;
            if (Physics.Raycast(transform.position, transform.forward, out hit, raycastDistance))
            {
                if (hit.collider.CompareTag("sheep") || hit.collider.CompareTag("Obstacle")) // Check if the obstacle is another sheep or an obstacle
                {
                    // Turn slightly to avoid the obstacle
                    float turnDirection = Random.Range(0, 2) == 0 ? -avoidTurnAngle : avoidTurnAngle;
                    StartCoroutine(Turn(turnDirection));
                    return; // Skip moving forward this frame
                }
            }

            // Move forward if no obstacle
            transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
        }

        private System.Collections.IEnumerator Turn(float angle)
        {
            // Set turning flag to true
            isTurning = true;
            isMoving = false; // Stop forward movement while turning

            // Play turn animation
            if (angle < 0)
            {
                animator.Play(turn90LAnimation);
            }
            else
            {
                animator.Play(turn90RAnimation);
            }

            // Smooth turning over time
            float targetRotation = transform.eulerAngles.y + angle;
            float currentRotation = transform.eulerAngles.y;

            while (Mathf.Abs(Mathf.DeltaAngle(currentRotation, targetRotation)) > 0.5f)
            {
                float step = turnSpeed * Time.deltaTime;
                currentRotation = Mathf.MoveTowardsAngle(currentRotation, targetRotation, step);
                transform.eulerAngles = new Vector3(0, currentRotation, 0);
                yield return null;
            }

            // Ensure final rotation matches exactly
            transform.eulerAngles = new Vector3(0, targetRotation, 0);

            // Reset turning flag
            isTurning = false;
        }

        // Draw a Gizmo to show the direction the sheep is pointing
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green; // Set the color of the Gizmo
            Vector3 forward = transform.forward * raycastDistance; // Get the forward direction scaled by raycast distance
            Gizmos.DrawLine(transform.position, transform.position + forward); // Draw a line from the sheep's position in the forward direction
            Gizmos.DrawSphere(transform.position + forward, 0.1f); // Draw a small sphere at the end of the line
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(playAreaCenter, playAreaSize);
        }
    }
}
