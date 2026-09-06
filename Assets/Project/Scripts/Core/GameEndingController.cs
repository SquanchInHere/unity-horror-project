using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public sealed class GameEndingController : MonoBehaviour
{
    [Header("Ending Trigger")]
    [SerializeField] private OpenObject finalDoor;

    [Header("Player")]
    [SerializeField] private PlayerInputReader playerInput;

    [Header("Ending Screen")]
    [SerializeField] private CanvasGroup endingCanvasGroup;
    [SerializeField] private GameObject gameplayHud;

    [Min(0.01f)]
    [SerializeField] private float fadeDuration = 2.0f;

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private Coroutine endingRoutine;
    private bool endingStarted;

    public bool EndingStarted => endingStarted;

    private void Awake()
    {
        ResolvePlayerInput();

        if (finalDoor == null)
        {
            Debug.LogError(
                "GameEndingController: Final Door is not assigned.",
                this
            );

            enabled = false;
            return;
        }

        if (endingCanvasGroup == null)
        {
            Debug.LogError(
                "GameEndingController: Ending Canvas Group is not assigned.",
                this
            );

            enabled = false;
            return;
        }

        PrepareHiddenScreen();
    }

    private void OnEnable()
    {
        if (finalDoor != null)
            finalDoor.Opened += BeginEnding;
    }

    private void OnDisable()
    {
        if (finalDoor != null)
            finalDoor.Opened -= BeginEnding;
    }

    public void BeginEnding()
    {
        if (endingStarted)
            return;

        endingStarted = true;

        if (endingRoutine != null)
            StopCoroutine(endingRoutine);

        endingRoutine = StartCoroutine(EndingRoutine());
    }

    public void LoadMainMenu()
    {
        if (string.IsNullOrWhiteSpace(mainMenuSceneName))
        {
            Debug.LogError(
                "GameEndingController: Main Menu Scene Name is empty.",
                this
            );

            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
        {
            Debug.LogError(
                $"GameEndingController: Scene '{mainMenuSceneName}' is not available in the build scene list.",
                this
            );

            return;
        }

        RestoreTimeBeforeSceneChange();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void RestartLevel()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        RestoreTimeBeforeSceneChange();
        SceneManager.LoadScene(currentSceneName);
    }

    public void QuitGame()
    {
        RestoreTimeBeforeSceneChange();

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator EndingRoutine()
    {
        ResolvePlayerInput();

        if (playerInput != null)
            playerInput.SetGameplayEnabled(false);

        if (gameplayHud != null)
            gameplayHud.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        endingCanvasGroup.gameObject.SetActive(true);
        endingCanvasGroup.alpha = 0.0f;
        endingCanvasGroup.interactable = false;
        endingCanvasGroup.blocksRaycasts = true;

        Time.timeScale = 0.0f;

        float elapsedTime = 0.0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsedTime / fadeDuration);
            endingCanvasGroup.alpha = Mathf.SmoothStep(0.0f, 1.0f, progress);

            yield return null;
        }

        endingCanvasGroup.alpha = 1.0f;
        endingCanvasGroup.interactable = true;
        endingCanvasGroup.blocksRaycasts = true;
        endingRoutine = null;
    }

    private void PrepareHiddenScreen()
    {
        endingCanvasGroup.alpha = 0.0f;
        endingCanvasGroup.interactable = false;
        endingCanvasGroup.blocksRaycasts = false;
        endingCanvasGroup.gameObject.SetActive(false);
    }

    private void ResolvePlayerInput()
    {
        if (playerInput != null)
            return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
            playerInput = playerObject.GetComponent<PlayerInputReader>();
    }

    private static void RestoreTimeBeforeSceneChange()
    {
        Time.timeScale = 1.0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
