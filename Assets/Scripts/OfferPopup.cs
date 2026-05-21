using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class OfferPopup : MonoBehaviour
{
    [Header("Prefabs by image format")]
    [SerializeField] private OfferPopupView verticalPrefab;    // вертикальная картинка
    [SerializeField] private OfferPopupView horizontalPrefab;  // горизонтальная картинка
    [SerializeField] private OfferPopupView squarePrefab;      // квадратная картинка

    [Header("Where to spawn")]
    [SerializeField] private Transform popupParent;            // Canvas/PopupsRoot и т.п.

    [Header("Format thresholds")]
    [Tooltip("Насколько картинка может отличаться от 1:1, чтобы считаться квадратной. Пример: 0.10 = 10%")]
    [Range(0.01f, 0.5f)]
    [SerializeField] private float squareTolerance = 0.10f; // 10%

    private OfferPopupView _current;
    private OfferRemoteConfigData _currentData;
    private Action _onOpen;

    public void Show(OfferRemoteConfigData data, Action onOpen)
    {
        if (data == null || string.IsNullOrEmpty(data.picture_url))
        {
            Debug.LogWarning("OfferPopup: data is null or picture_url is empty.");
            return;
        }

        _currentData = data;
        _onOpen = onOpen;

        StartCoroutine(LoadAndShow(data.picture_url));
    }

    public void Hide()
    {
        if (_current != null)
        {
            Destroy(_current.gameObject);
            _current = null;
        }
    }

    public void OpenAndClose()
    {
        _onOpen?.Invoke();
    }

    private IEnumerator LoadAndShow(string url)
    {
        using var req = UnityWebRequestTexture.GetTexture(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"OfferPopup: Failed to load image. {req.error} url={url}");
            yield break;
        }

        var tex = DownloadHandlerTexture.GetContent(req);
        if (tex == null || tex.width == 0 || tex.height == 0)
        {
            Debug.LogError("OfferPopup: Texture is null or has invalid size.");
            yield break;
        }

        // width/height: >1 горизонтальная, <1 вертикальная
        float aspect = (float)tex.width / tex.height;

        OfferPopupView prefab = ChoosePrefabByFormat(aspect);
        if (prefab == null)
        {
            Debug.LogWarning("OfferPopup: Prefab for image format is not assigned.");
            yield break;
        }

        Hide();

        _current = Instantiate(prefab, popupParent);
        _current.SetImage(tex);

        var actions = _current.GetComponent<OfferPopupActions>();
        if (actions != null)
        {
            actions.Bind(_currentData, OpenAndClose, Hide);
        }

        // Debug.Log($"OfferPopup: Loaded {tex.width}x{tex.height}, aspect={aspect:F2}");
    }

    private OfferPopupView ChoosePrefabByFormat(float aspect)
    {
        // square if close to 1:1
        if (Mathf.Abs(aspect - 1f) <= squareTolerance)
            return squarePrefab;

        // horizontal if wider than tall
        if (aspect > 1f)
            return horizontalPrefab;

        // vertical otherwise
        return verticalPrefab;
    }
}
