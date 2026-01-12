using UnityEngine;

public class PlayerFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float speed = 15;
    [SerializeField] Vector3 delta = Vector3.zero;
    [SerializeField] private float eps = 0.01f;
    [SerializeField] private Animator animationController;
    
    private Vector3 oldPos = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);

    private void Update()
    {
        Vector3 rawPosition = target.position + delta;

        animationController.SetBool("isMoving", (rawPosition - oldPos).magnitude > eps);
        oldPos = rawPosition;
        
        transform.position = Vector3.Lerp(transform.position, rawPosition, speed * Time.deltaTime);
        transform.rotation = target.rotation;
    }
}