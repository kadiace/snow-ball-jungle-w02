using UnityEngine;

public class ResourceController : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        PlayerController player = other.GetComponent<PlayerController>();

        if (gameObject.CompareTag("Tree"))
            Managers.Game.Woods.Add(gameObject);
        if (gameObject.CompareTag("Metal"))
            Managers.Game.Irons.Add(gameObject);

        transform.SetParent(other.transform);
    }
}
