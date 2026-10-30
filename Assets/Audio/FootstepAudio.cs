
using UnityEngine;

public class FootstepAudio : MonoBehaviour
{
    [Header("Wwise")]
    public AK.Wwise.Event footstepEvent;

    [Header("Footstep Settings")]
    public float stepInterval = 0.5f;

    private float stepTimer;

    void Update()
    {
        // Check whether the player is moving
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        bool isMoving = horizontal != 0 || vertical != 0;

        if (isMoving)
        {
            stepTimer += Time.deltaTime;

            if (stepTimer >= stepInterval)
            {
                footstepEvent.Post(gameObject);
                stepTimer = 0f;
            }
        }
        else
        {
            stepTimer = 0f;
        }
    }
}
