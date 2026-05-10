using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Guarantees exactly one enabled EventSystem with StandaloneInputModule across scene loads (works with inactive/disabled leftovers).
/// </summary>
public static class UiInputEnsure
{
    public static void Bootstrap()
    {
        EventSystem[] systems = Object.FindObjectsByType<EventSystem>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        EventSystem keeper = EventSystem.current != null ? EventSystem.current : null;

        if (keeper == null && systems.Length > 0)
            keeper = systems[0];

        for (int i = 0; i < systems.Length; i++)
        {
            EventSystem candidate = systems[i];

            if (candidate == null || candidate == keeper)
                continue;

            Object.Destroy(candidate.gameObject);
        }

        if (keeper == null)
        {
            GameObject shell = new GameObject("EventSystem");
            keeper = shell.AddComponent<EventSystem>();
            shell.AddComponent<StandaloneInputModule>();
            Object.DontDestroyOnLoad(shell);
        }

        keeper.gameObject.SetActive(true);
        keeper.enabled = true;

        StandaloneInputModule legacy = keeper.GetComponent<StandaloneInputModule>();
        if (legacy == null)
            legacy = keeper.gameObject.AddComponent<StandaloneInputModule>();

        legacy.enabled = true;
    }
}
