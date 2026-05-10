using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Optional fallback: drop this script on ONE empty GameObject in MainMenu / Level scenes.
/// It forces MandatoryCourseUi to rebuild for that scene (covers rare bootstrap-order issues).
/// </summary>
[DefaultExecutionOrder(-10000)]
public sealed class MandatoryUiSceneHook : MonoBehaviour
{
    void Awake()
    {
        MandatoryCourseUi.Ensure();
        Scene s = gameObject.scene;
        if (MandatoryCourseUi.Instance != null && s.IsValid())
            MandatoryCourseUi.Instance.RebuildForScene(s);
    }
}
