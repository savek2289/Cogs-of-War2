using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadScene : MonoBehaviour
{
    [SerializeField] string sceneName;

    [SerializeField] bool canLoadScene = false;

    public void LoadSceneAsync()
    {
        if (sceneName == null || sceneName.Length == 0) return;

        StartCoroutine(Loading());
    }

    public void SetCanLoadScene(bool parametr) => canLoadScene = parametr;

    private IEnumerator Loading()
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = canLoadScene;

        yield return new WaitUntil(() => canLoadScene == true);

        operation.allowSceneActivation = true;
    }
}
