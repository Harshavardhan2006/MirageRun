using TMPro;
using UnityEngine;

public class MenuControl : MonoBehaviour
{
    [SerializeField] TMP_Text coinText;
    [SerializeField] TMP_Text gemText;
    [SerializeField] TMP_Text bestText;
    [SerializeField] TMP_Text soundText;

    void Start()
    {
        if (coinText != null) coinText.text = PlayerPrefs.GetInt("COINSAVE", 0).ToString();
        if (gemText != null) gemText.text = PlayerPrefs.GetInt("GEMSAVE", 0).ToString();
        if (bestText != null) bestText.text = PlayerPrefs.GetInt("BESTDISTANCE", 0) + " m";
        RefreshSound();
    }

    public void LoadScene(int buildIndex)
    {
        AudioManager.PlayClick();
        ScreenFader.Go(buildIndex);
    }

    public void ToggleSound()
    {
        AudioManager.ToggleMute();
        AudioManager.PlayClick();
        RefreshSound();
    }

    void RefreshSound()
    {
        if (soundText != null) soundText.text = AudioManager.Muted ? "SOUND: OFF" : "SOUND: ON";
    }

    public void Quit()
    {
        AudioManager.PlayClick();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
