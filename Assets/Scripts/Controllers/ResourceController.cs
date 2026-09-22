using UnityEngine;

public class ResourceController : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (gameObject.CompareTag("Tree"))
            Managers.Game.Woods.Add(gameObject);
        if (gameObject.CompareTag("Metal"))
            Managers.Game.Irons.Add(gameObject);

        transform.position = other.transform.position +
            (transform.position - other.transform.position).normalized * (other.transform.localScale.x / 2 + 1f);
        transform.SetParent(other.transform);
    }
}
