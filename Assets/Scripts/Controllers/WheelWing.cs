using UnityEngine;

public class WheelWing : MonoBehaviour
{
    private Wheel parent;

    void Awake()
    {
        parent = GetComponentInParent<Wheel>();
    }
    private void OnCollisionStay(Collision other)
    {

        if (other.gameObject.name != "Player")
            return;

        parent.SetRigidbodyConstraintsAllWings(true);
    }

    private void OnCollisionExit(Collision other)
    {
        if (other.gameObject.name != "Player")
            return;

        parent.SetRigidbodyConstraintsAllWings(false);
    }

}
