using UnityEngine;

public class StayOnTop : MonoBehaviour
{
    public Transform land;
    public LayerMask landLayer;

    void LateUpdate()
    {
        // Cast a ray downwards from above the bonfire
        Ray ray = new Ray(transform.position + Vector3.up * 5f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 20f, landLayer))
        {
            // Stick bonfire to the surface of the land
            transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);
        }
        else if (Physics.Raycast(ray, out hit, 20f))
        {
            // If layerMask didn't hit, try with Land tag
            if (hit.collider.CompareTag("Land"))
            {
                transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);
            }
        }
    }
}