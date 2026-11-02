using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] float laneWidth = 3f;
    [SerializeField] float laneSnap = 14f;
    [SerializeField] float maxSideSpeed = 26f;
    [SerializeField] float jumpHeight = 1.7f;
    [SerializeField] Transform model;
    [SerializeField] Animator animator;

    Rigidbody rb;
    int lane;
    float jumpBuffer;
    bool dead;

    public bool Grounded { get; private set; }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        if (animator == null && model != null) animator = model.GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (dead) return;
        MasterLevelInfo.distanceRun = Mathf.Max(0, Mathf.FloorToInt(transform.position.z));
        if (!GameManager.IsPlaying)
        {
            if (!GameManager.IsPaused) SetAnim(0f);
            return;
        }
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) lane = Mathf.Max(-1, lane - 1);
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) lane = Mathf.Min(1, lane + 1);
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) jumpBuffer = 0.15f;
        jumpBuffer -= Time.deltaTime;
        SetAnim(Mathf.Clamp(GameManager.Speed / 11f, 0.8f, 2.2f));
    }

    void FixedUpdate()
    {
        Vector3 v = rb.linearVelocity;
        Grounded = Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, 0.35f, ~0, QueryTriggerInteraction.Ignore);

        if (dead || !GameManager.IsPlaying)
        {
            rb.linearVelocity = new Vector3(0f, v.y, 0f);
            return;
        }

        float dx = lane * laneWidth - rb.position.x;
        v.x = Mathf.Clamp(dx * laneSnap, -maxSideSpeed, maxSideSpeed);
        v.z = GameManager.Speed;

        if (Grounded && jumpBuffer > 0f)
        {
            v.y = Mathf.Sqrt(2f * -Physics.gravity.y * jumpHeight);
            jumpBuffer = 0f;
            AudioManager.PlayJump();
        }

        rb.linearVelocity = v;
    }

    void SetAnim(float value)
    {
        if (animator != null) animator.SetFloat("RunSpeed", value);
    }

    public void Die()
    {
        if (dead) return;
        dead = true;
        jumpBuffer = 0f;
        if (animator != null) animator.speed = 0f;
        RunBob bob = GetComponentInChildren<RunBob>();
        if (bob != null) bob.enabled = false;
        if (model != null) StartCoroutine(Fall());
    }

    IEnumerator Fall()
    {
        Quaternion from = model.localRotation;
        Quaternion to = Quaternion.Euler(-80f, 0f, 0f) * from;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.4f;
            model.localRotation = Quaternion.Slerp(from, to, t * t);
            yield return null;
        }
    }
}
