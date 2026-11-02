using UnityEngine;

public class Spin : MonoBehaviour
{
    [SerializeField] float speed = 30f;

    void Update()
    {
        transform.Rotate(0f, speed * Time.deltaTime, 0f, Space.World);
    }
}
