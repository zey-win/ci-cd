using Firebase.Analytics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FirebaseInit : MonoBehaviour
{
    private const string UserBalanceKey = "Balance";

    private void Start()
    {
        if (!PlayerPrefs.HasKey(UserBalanceKey))
        {
            int startBalance = RemoteConfigManager.GetInt(RemoteConfigManager.START_BALANCE, 0);
            PlayerPrefs.SetFloat(UserBalanceKey, startBalance);
            PlayerPrefs.Save();
        }

        AnalyticsSafe.LogEvent("user_routed", new Parameter("destination", "game"));
        SceneManager.LoadScene("Game");
    }
}
