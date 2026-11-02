using System.Collections.Generic;
using UnityEngine;

public class SegmentGenerator : MonoBehaviour
{
    [SerializeField] GameObject startSegment;
    [SerializeField] GameObject[] segments;
    [SerializeField] Transform player;
    [SerializeField] float segmentLength = 60f;
    [SerializeField] int segmentsAhead = 4;
    [SerializeField] float despawnBehind = 50f;

    readonly Queue<GameObject> live = new Queue<GameObject>();
    float nextZ;
    int lastIndex = -1;

    void Start()
    {
        nextZ = -segmentLength * 0.5f;
        if (startSegment != null) Place(startSegment);
        while (live.Count < segmentsAhead) Spawn();
    }

    void Update()
    {
        while (nextZ < player.position.z + segmentLength * segmentsAhead) Spawn();
        while (live.Count > 0 && live.Peek().transform.position.z + segmentLength < player.position.z - despawnBehind)
        {
            Destroy(live.Dequeue());
        }
    }

    void Spawn()
    {
        if (segments == null || segments.Length == 0) return;
        int index = Random.Range(0, segments.Length);
        if (segments.Length > 1 && index == lastIndex) index = (index + 1 + Random.Range(0, segments.Length - 1)) % segments.Length;
        lastIndex = index;
        Place(segments[index]);
    }

    void Place(GameObject prefab)
    {
        live.Enqueue(Instantiate(prefab, new Vector3(0f, 0f, nextZ), Quaternion.identity, transform));
        nextZ += segmentLength;
    }
}
