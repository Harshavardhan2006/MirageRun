using TMPro;
using UnityEngine;

public class MasterLevelInfo : MonoBehaviour
{
    public static int coinCount;
    public static int gemCount;
    public static int distanceRun;

    public static int Score { get { return distanceRun + coinCount * 10 + gemCount * 50; } }

    [SerializeField] TMP_Text coinText;
    [SerializeField] TMP_Text gemText;
    [SerializeField] TMP_Text distanceText;
    [SerializeField] TMP_Text scoreText;

    public static void ResetAll()
    {
        coinCount = 0;
        gemCount = 0;
        distanceRun = 0;
    }

    void Awake()
    {
        ResetAll();
    }

    void Update()
    {
        if (coinText != null) coinText.text = coinCount.ToString();
        if (gemText != null) gemText.text = gemCount.ToString();
        if (distanceText != null) distanceText.text = distanceRun + " m";
        if (scoreText != null) scoreText.text = Score.ToString("N0");
    }
}
