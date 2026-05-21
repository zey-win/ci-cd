using UnityEngine;

public class View : MonoBehaviour
{
    public void Init(string url)
    {
        if (!string.IsNullOrEmpty(url))
            Application.OpenURL(url);

        Destroy(gameObject);
    }
}
