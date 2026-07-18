using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransitionManager : MonoBehaviour
{
    private static SceneTransitionManager _instance;

    [SerializeField] [Tooltip("Screen fade controller for fade animations")]
    private ScreenFadeController screenFadeController;

    private bool _isTransitioning = false;

    public static SceneTransitionManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<SceneTransitionManager>();

                if (_instance == null)
                {
                    GameObject managerGO = new GameObject("SceneTransitionManager");
                    _instance = managerGO.AddComponent<SceneTransitionManager>();
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (screenFadeController == null)
        {
            screenFadeController = GetComponentInChildren<ScreenFadeController>();

            if (screenFadeController == null)
            {
                Debug.LogWarning($"{gameObject.name}: ScreenFadeController not found. Scene transitions will proceed without fade animation.");
            }
        }
    }

    private void OnEnable()
    {
    }

    public bool IsTransitioning => _isTransitioning;

    public void LoadScene(string sceneName)
    {
        if (_isTransitioning)
        {
            Debug.LogWarning("Scene transition already in progress. Ignoring new transition request.");
            return;
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("Scene name cannot be null or empty");
            return;
        }

        StartCoroutine(LoadSceneCoroutine(sceneName, 0f));
    }

    public void LoadScene(string sceneName, float delay)
    {
        if (_isTransitioning)
        {
            Debug.LogWarning("Scene transition already in progress. Ignoring new transition request.");
            return;
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("Scene name cannot be null or empty");
            return;
        }

        if (delay < 0f)
        {
            Debug.LogWarning("Delay cannot be negative. Using 0.");
            delay = 0f;
        }

        StartCoroutine(LoadSceneCoroutine(sceneName, delay));
    }

    public void ReloadCurrentScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        LoadScene(currentSceneName);
    }

    public void ReloadCurrentScene(float delay)
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        LoadScene(currentSceneName, delay);
    }

    private IEnumerator LoadSceneCoroutine(string sceneName, float delay)
    {
        _isTransitioning = true;

        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        if (screenFadeController != null)
        {
            yield return screenFadeController.FadeOut();
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        if (screenFadeController != null)
        {
            yield return screenFadeController.FadeIn();
        }

        _isTransitioning = false;
    }
}
