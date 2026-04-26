using System.Collections;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public Animator animator;
    public float moveSpeed = 3f;
    public Vector2 movement;

    void Update()
    {
        HandleMovement();
    }

    public void HandleMovement()
    {
        float input = Input.GetAxis("Horizontal");
        movement.x = input * moveSpeed * Time.deltaTime;
        transform.Translate(movement);

        // Fixed: true when moving, false when idle
        animator.SetBool("Walking", input != 0);
    }
}