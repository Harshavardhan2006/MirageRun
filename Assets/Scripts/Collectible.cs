using System.Collections;
using UnityEngine;

public class Collectible : MonoBehaviour
{
    public enum Kind { Coin, Gem }

    [SerializeField] Kind kind;
    [SerializeField] float spinSpeed = 140f;
    [SerializeField] float riseSpeed = 7f;
    [SerializeField] float shrinkTime = 0.35f;

    bool collected;

    void Update()
    {
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        if (collected) transform.Translate(Vector3.up * riseSpeed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        if (collected || !other.CompareTag("Player")) return;
        collected = true;

        if (kind == Kind.Coin)
        {
            MasterLevelInfo.coinCount++;
            AudioManager.PlayCoin();
            Fx.Burst(transform.position, new Color(1f, 0.85f, 0.2f));
        }
        else
        {
            MasterLevelInfo.gemCount++;
            AudioManager.PlayGem();
            Fx.Burst(transform.position, new Color(1f, 0.2f, 0.3f));
        }
        StartCoroutine(ShrinkAndHide());
    }

    IEnumerator ShrinkAndHide()
    {
        Vector3 start = transform.localScale;
        float t = 0f;
        while (t < shrinkTime)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(start, Vector3.zero, t / shrinkTime);
            yield return null;
        }
        gameObject.SetActive(false);
    }
}
