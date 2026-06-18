using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class LoadingSceneController : MonoBehaviour
{
    [Header("UI")]
    public Slider progressBar;

    private IEnumerator Start()
    {
        AsyncOperation operation =
            SceneManager.LoadSceneAsync(SceneLoader.sceneToLoad);

        operation.allowSceneActivation = true;

        while (!operation.isDone)
        {
            float progress =
                Mathf.Clamp01(operation.progress / 0.9f);

            if (progressBar != null)
                progressBar.value = progress;

            yield return null;
        }
    }
}