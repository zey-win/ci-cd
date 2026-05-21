using UnityEngine;
using Firebase.Messaging;

public class PushPermissionManager : MonoBehaviour
{
    [Header("UI")]
    public PushRequestCanvas permissionPopup;      // корневой объект твоего попапа

    private PushRequestCanvas _permissionPopupInstance;

    private const string PrefsKey = "PushPermissionAsked";

    private void Start()
    {
#if UNITY_IOS || UNITY_ANDROID
        // Показываем свой попап только если ещё ни разу не спрашивали
        if (PlayerPrefs.GetInt(PrefsKey, 0) == 0)
        {
            if (permissionPopup == null) return;

            _permissionPopupInstance = Instantiate(permissionPopup);
            _permissionPopupInstance.Init(OnConfirmButton, OnSkipButton);
        }
        else
        {
            // Уже спрашивали раньше → можно просто попробовать сразу создать FirebasePush
            TryInitFirebasePushIfPermissionAlreadyGranted();
        }
#else
        // На других платформах пуши не спрашиваем
        CreateFirebasePush();
#endif
    }

    // Кнопка "Skip" в твоём UI
    public void OnSkipButton()
    {
        PlayerPrefs.SetInt(PrefsKey, 1);
        PlayerPrefs.Save();

        if (_permissionPopupInstance != null)
            Destroy(_permissionPopupInstance.gameObject);

        // Если хочешь – можешь всё равно инициализировать Firebase,
        // токен будет, но пуши не будут отображаться, пока юзер сам не включит их в настройках ОС.
        // CreateFirebasePush();
    }


    // Кнопка "Confirm" в твоём UI
    public async void OnConfirmButton()
    {
        PlayerPrefs.SetInt(PrefsKey, 1);
        PlayerPrefs.Save();

        if (_permissionPopupInstance != null)
            Destroy(_permissionPopupInstance.gameObject);

        bool permissionGranted = false;

#if UNITY_IOS
        try
        {
            // На iOS именно этот вызов покажет системный диалог (если его ещё не было)
            var task = FirebaseMessaging.RequestPermissionAsync();
            await task;

            // Если без ошибок – считаем, что всё ок
            if (!task.IsFaulted && !task.IsCanceled)
            {
                Debug.Log("iOS push permission requested");
                permissionGranted = true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"RequestPermissionAsync iOS failed: {e}");
        }

#elif UNITY_ANDROID
        const string notificationPermission = "android.permission.POST_NOTIFICATIONS";

        // Если уже есть разрешение – просто используем его
        if (UnityEngine.Android.Permission.HasUserAuthorizedPermission(notificationPermission))
        {
            permissionGranted = true;
        }
        else
        {
            // Показываем системный диалог Android 13+
            var callbacks = new UnityEngine.Android.PermissionCallbacks();
            bool finished = false;

            callbacks.PermissionGranted += permissionName =>
            {
                Debug.Log("Android notification permission granted");
                permissionGranted = true;
                finished = true;
            };
            callbacks.PermissionDenied += permissionName =>
            {
                Debug.LogWarning("Android notification permission denied");
                finished = true;
            };
            callbacks.PermissionDeniedAndDontAskAgain += permissionName =>
            {
                Debug.LogWarning("Android notification permission denied and don't ask again");
                finished = true;
            };

            UnityEngine.Android.Permission.RequestUserPermission(notificationPermission, callbacks);

            // ждём колбэка
            while (!finished)
                await System.Threading.Tasks.Task.Yield();
        }
#endif

        if (permissionGranted)
        {
            CreateFirebasePush();
        }
        else
        {
            Debug.LogWarning("Permission not granted → FirebasePush will not be created");
        }
    }

    private void TryInitFirebasePushIfPermissionAlreadyGranted()
    {
        bool permissionGranted = false;

#if UNITY_IOS
        // На iOS можно просто попробовать снова запросить – если юзер уже отвечал,
        // системный диалог не появится, а статус вернётся сразу.
        var task = FirebaseMessaging.RequestPermissionAsync();
        task.ContinueWith(t =>
        {
            if (!t.IsFaulted && !t.IsCanceled)
            {
                permissionGranted = true;
                if (permissionGranted)
                    CreateFirebasePush();
            }
        });
#elif UNITY_ANDROID
        const string notificationPermission = "android.permission.POST_NOTIFICATIONS";
        permissionGranted = UnityEngine.Android.Permission.HasUserAuthorizedPermission(notificationPermission);
        if (permissionGranted)
            CreateFirebasePush();
#endif
    }

    private void CreateFirebasePush()
    {
        Debug.Log("Permission granted → Adding FirebasePush to this GameObject");

        if (GetComponent<FirebasePush>() == null)
        {
            gameObject.AddComponent<FirebasePush>();
        }

        if (GetComponent<GameLocalNotifications>() == null)
        {
            gameObject.AddComponent<GameLocalNotifications>();
        }
    }
}
