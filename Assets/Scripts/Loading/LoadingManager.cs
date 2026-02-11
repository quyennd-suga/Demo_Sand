using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LoadingManager : MonoBehaviour
{
    [SerializeField]
    private Image progress;

    [SerializeField]
    private string gameSceneAddress = "Game"; // Scene name in Build Settings

    void Start()
    {
        StartCoroutine(LoadGameScene());
    }

    IEnumerator LoadGameScene()
    {
        // Enforce minimum loading visual duration
        float minDisplayTime = 1.5f;// 0.75f;
        float elapsedTime = 0f;

        // Start async loading the scene but DO NOT auto-activate it
        AsyncOperation op = SceneManager.LoadSceneAsync(gameSceneAddress, LoadSceneMode.Single);
        op.allowSceneActivation = false;

        float shownProgress = 0f;

        while (!op.isDone)
        {
            elapsedTime += Time.deltaTime;

            // Real load progress (0..0.9), normalize to (0..1)
            float loadProgress = Mathf.Clamp01(op.progress / 0.9f);

            // Minimum display time progress (0..1)
            float timeProgress = Mathf.Clamp01(elapsedTime / minDisplayTime);

            // Show whichever is bigger so bar reflects real loading but respects min time
            float target = Mathf.Max(loadProgress, timeProgress);

            // Optional smoothing so bar doesn't jitter
            shownProgress = Mathf.MoveTowards(shownProgress, target, Time.deltaTime * 2f);

            progress.fillAmount = shownProgress;

            // Ready to activate when scene finished loading (0.9) AND min time passed
            if (op.progress >= 0.9f && elapsedTime >= minDisplayTime)
            {
                Debug.Log("Loading complete, activating scene.");

                progress.fillAmount = 1f;
                op.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}
