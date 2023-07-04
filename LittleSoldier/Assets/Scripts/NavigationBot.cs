using UnityEngine;
using UnityEngine.AI;

public class NavigationBot : MonoBehaviour
{
    public float rotationDamping = 0.4f;
    public NavMeshAgent navMeshAgent;
    public Animator animator;

    private void OnEnable()
    {
        navMeshAgent.updateRotation = false;
    }

    public void SetDestination(Vector3 destination)
    {
        navMeshAgent.SetDestination(destination);
    }
    
    private void Update()
    {
        float speed = navMeshAgent.velocity.magnitude;
        animator.SetFloat("speed", speed);
        if (navMeshAgent.velocity.magnitude / navMeshAgent.desiredVelocity.magnitude > 0.1f)
        {
            gameObject.transform.rotation = Quaternion.Slerp(gameObject.transform.rotation, 
                Quaternion.LookRotation(navMeshAgent.velocity), rotationDamping);            
        }
    }
}
