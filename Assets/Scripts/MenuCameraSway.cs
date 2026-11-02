using UnityEngine;

public class MenuCameraSway : MonoBehaviour
{
    [SerializeField] Transform focus;
    [SerializeField] Vector3 offset = new Vector3(0f, 0.5f, 4.6f);
    [SerializeField] float swing = 22f;
    [SerializeField] float speed = 0.45f;

    void LateUpdate()
    {
        if (focus == null) return;
        Quaternion rot = Quaternion.Euler(0f, Mathf.Sin(Time.time * speed) * swing, 0f);
        transform.position = focus.position + rot * offset;
        transform.LookAt(focus.position + Vector3.up * 0.95f);
    }
}
