using System.Collections.Generic;
using UnityEngine;

public class AnimationController : MonoBehaviour
{
    public Animator animator;
    public string walkForwardAnimation = "walk_forward";
    public string walkBackwardAnimation = "walk_backwards";
    public string runForwardAnimation = "run_forward";
    public string turn90LAnimation = "turn_90_L";
    public string turn90RAnimation = "turn_90_R";
    public string trotAnimation = "trot_forward";

    private List<string> animations;
    private float timer;
    private float interval = 2.0f; // Change animation every 2 seconds

    private float walkSpeed = 2.0f;
    private float runSpeed = 4.0f;
    private float turnSpeed = 90.0f; // degrees per second

    // Define the boundaries as a square area
    [SerializeField]
    private Vector3 areaCenter = new Vector3(0, 0, 0);
    [SerializeField]
    private float areaSize = 20.0f;

    void Start()
    {
        animator = GetComponent<Animator>();
        animations = new List<string>
        {
            walkForwardAnimation,
            runForwardAnimation,
            turn90LAnimation,
            turn90RAnimation,
            trotAnimation
        };
        timer = interval;
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            PlayRandomAnimation();
            timer = interval;
        }

        MoveSheep();
    }

    void PlayRandomAnimation()
    {
        int randomIndex = Random.Range(0, animations.Count);
        animator.Play(animations[randomIndex]);
    }

    void MoveSheep()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        Vector3 newPosition = transform.position;
    
        if (stateInfo.IsName(walkForwardAnimation))
        {
            newPosition += transform.forward * walkSpeed * Time.deltaTime;
        }
        else if (stateInfo.IsName(runForwardAnimation))
        {
            newPosition += transform.forward * runSpeed * Time.deltaTime;
        }
        else if (stateInfo.IsName(turn90LAnimation))
        {
            transform.Rotate(Vector3.up, -turnSpeed * Time.deltaTime);
        }
        else if (stateInfo.IsName(turn90RAnimation))
        {
            transform.Rotate(Vector3.up, turnSpeed * Time.deltaTime);
        }
        else if (stateInfo.IsName(trotAnimation))
        {
            newPosition += transform.forward * walkSpeed * Time.deltaTime;
        }
    
        // Check if the new position is within the square boundaries
        float halfSize = areaSize / 2.0f;
        if (newPosition.x >= areaCenter.x - halfSize && newPosition.x <= areaCenter.x + halfSize &&
            newPosition.z >= areaCenter.z - halfSize && newPosition.z <= areaCenter.z + halfSize)
        {
            transform.position = newPosition;
        }
        else
        {
            // Optionally, make the sheep turn around if it hits the boundary
            transform.Rotate(Vector3.up, 180);
        }
    }

    void OnDrawGizmos()
    {
        // Draw a wireframe box to visualize the boundaries
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(areaCenter, new Vector3(areaSize, 1, areaSize));
    }
}
