using UnityEngine;

public class AutoLoadScene : MonoBehaviour
{
    [SerializeField] int sceneIndex = 3;
    [SerializeField] float delay = 5f;
    [SerializeField] RectTransform bar;

    float timer;
    bool loading;

    void Update()
    {
        if (loading) return;
        timer += Time.deltaTime;
        if (bar != null) bar.localScale = new Vector3(Mathf.Clamp01(timer / delay), 1f, 1f);
        bool skip = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0);
        if (timer >= delay || (timer > 0.8f && skip))
        {
            loading = true;
            ScreenFader.Go(sceneIndex);
        }
    }
}
