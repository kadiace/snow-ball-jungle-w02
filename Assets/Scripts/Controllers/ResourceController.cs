using UnityEngine;

public class ResourceController : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || Managers.Game.ResourcesData.Mass > 10)
            return;

        PlayerController player = other.GetComponent<PlayerController>();

        if (gameObject.CompareTag("Tree"))
            Managers.Game.ResourcesData.Tree += 1;
        if (gameObject.CompareTag("Metal"))
            Managers.Game.ResourcesData.Metal += 1;

        player.RequestSizeChange();

        transform.SetParent(other.transform);
    }
}
