using UnityEngine;

public class Rotate : MonoBehaviour
{
    void Update()
    {
        transform.Rotate(0, 0, -180 * Time.deltaTime, Space.Self);
    }
}
