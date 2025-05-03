using UnityEngine;

public class TeleportPlayer : MonoBehaviour
{
    public GameObject targetObject;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Vector3 targetPosition = targetObject.transform.position + new Vector3(0, 2, 0);

            collision.transform.position = targetPosition;
        }
    }
}
