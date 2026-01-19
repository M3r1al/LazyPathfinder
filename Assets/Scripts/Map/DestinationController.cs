using System.Collections;
using UnityEngine;

public class DestinationController : MonoBehaviour
{
    [SerializeField] Vector2 position;
    [SerializeField] private GameObject endPanel;
    private bool endedGame = false;

    private IEnumerator Start()
    {
        endPanel?.SetActive(false);
        yield return null;
        transform.position = new Vector3(position.x, LazyHeightSequence.TryGetHeight(position).height * MapStatic.PlaneSize, position.y);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (endedGame || !other.CompareTag("Player"))
            return;
        
        Time.timeScale = 0;
        endPanel?.SetActive(true);
        endedGame = true;
    }
}
