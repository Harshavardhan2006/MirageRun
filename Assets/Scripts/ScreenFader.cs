using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScreenFader : MonoBehaviour
{
    static ScreenFader instance;

    [SerializeField] CanvasGroup group;
    [SerializeField] float duration = 0.55f;

    void Awake()
    {
        instance = this;
        group.alpha = 1f;
        group.blocksRaycasts = false;
        group.interactable = false;
    }

    void Start()
    {
        StartCoroutine(Fade(1f, 0f));
    }

    public static void Go(int sceneIndex)
    {
        if (instance == null)
        {
            SceneManager.LoadScene(sceneIndex);
            return;
        }
        instance.StartCoroutine(instance.Out(sceneIndex));
    }

    IEnumerator Out(int sceneIndex)
    {
        group.blocksRaycasts = true;
        yield return Fade(group.alpha, 1f);
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneIndex);
    }

    IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        group.alpha = to;
    }
}
