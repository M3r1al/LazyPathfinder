using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float speed = 10;
    [SerializeField] Vector3 delta = Vector3.zero;

    private void Start()
    {
        if (delta == Vector3.zero)
            delta = transform.position - target.position;
    }

    private void Update()
    {
        Vector3 rawPosition = target.position + delta;
        transform.position = Vector3.Lerp(transform.position, rawPosition, speed * Time.deltaTime);
    }
}
