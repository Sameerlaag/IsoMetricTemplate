using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class MonsterClickMove : MonoBehaviour
{
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;
    [SerializeField] private Camera mainCamera;

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                agent.isStopped = false;
                agent.SetDestination(hit.point);
            }
        }

        animator.SetFloat("Speed", agent.velocity.magnitude);
    }
}