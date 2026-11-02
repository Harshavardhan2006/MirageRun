using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Vector3 offset = new Vector3(0f, 3.4f, -7.5f);
    [SerializeField] float lookAhead = 8f;

    static float shake;
    Camera cam;
    float x;
    float y;

    void Awake()
    {
        cam = GetComponent<Camera>();
        shake = 0f;
    }

    void Start()
    {
        if (target != null)
        {
            x = target.position.x * 0.7f;
            y = offset.y;
        }
    }

    public static void Shake(float amount)
    {
        shake = Mathf.Max(shake, amount);
    }

    void LateUpdate()
    {
        if (target == null) return;
        float dt = Time.deltaTime;
        x = Mathf.Lerp(x, target.position.x * 0.7f, dt * 7f);
        y = Mathf.Lerp(y, offset.y + target.position.y * 0.45f, dt * 6f);

        Vector3 pos = new Vector3(x, y, target.position.z + offset.z);
        if (shake > 0.001f)
        {
            pos += Random.insideUnitSphere * shake * 0.35f;
            shake = Mathf.MoveTowards(shake, 0f, dt * 1.4f);
        }
        transform.position = pos;
        transform.LookAt(new Vector3(target.position.x * 0.85f, 1.3f, target.position.z + lookAhead));

        float targetFov = 62f + Mathf.Clamp(GameManager.Speed - 11f, 0f, 15f) * 0.7f;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, dt * 2f);
    }
}
