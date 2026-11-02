using UnityEngine;

public class RunBob : MonoBehaviour
{
    [SerializeField] float frequency = 2.4f;
    [SerializeField] float bobHeight = 0.14f;
    [SerializeField] float lean = 9f;
    [SerializeField] float sway = 6f;

    Vector3 basePosition;
    Quaternion baseRotation;
    PlayerMovement player;
    float phase;
    float amount;

    void Awake()
    {
        basePosition = transform.localPosition;
        baseRotation = transform.localRotation;
        player = GetComponentInParent<PlayerMovement>();
    }

    void Update()
    {
        bool running = GameManager.Instance == null || GameManager.IsPlaying;
        bool airborne = player != null && !player.Grounded;
        float target = running ? (airborne ? 0.4f : 1f) : 0f;
        amount = Mathf.MoveTowards(amount, target, Time.deltaTime * 6f);
        float speedFactor = GameManager.Instance == null ? 1f : Mathf.Clamp(GameManager.Speed / 11f, 0.8f, 2.2f);
        phase += Time.deltaTime * frequency * speedFactor * Mathf.PI * 2f;

        float s = Mathf.Sin(phase);
        float bounce = Mathf.Abs(Mathf.Sin(phase)) * bobHeight * amount;
        transform.localPosition = basePosition + new Vector3(0f, bounce, 0f);
        transform.localRotation = baseRotation * Quaternion.Euler(lean * amount, s * 4f * amount, s * sway * amount);
    }
}
