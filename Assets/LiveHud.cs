using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class LiveHud : MonoBehaviour
{
    Text readout;
    HeroKnight trackedHero;

    public void Bind(Text label)
    {
        readout = label;
        trackedHero = null;
    }

    void LateUpdate()
    {
        if (readout == null)
            return;

        if (trackedHero == null)
            trackedHero = Object.FindFirstObjectByType<HeroKnight>();

        HeroKnight hero = trackedHero;

        if (hero == null)
        {
            readout.text = string.Empty;

            return;
        }

        int current = Mathf.Max(1, Mathf.RoundToInt(hero.GetHealthCurrent()));
        int cap = Mathf.Max(1, Mathf.RoundToInt(hero.GetHealthMax()));

        Scene s = SceneManager.GetActiveScene();
        readout.text = $"{DescribeLevelLine(s)}\n{current}/{cap}";
    }

    static string DescribeLevelLine(Scene s)
    {
        if (s.buildIndex >= 1)
            return $"Level {s.buildIndex}";

        string n = s.name;
        if (n.StartsWith("Level", System.StringComparison.OrdinalIgnoreCase))
        {
            string tail = n.Substring("Level".Length);
            if (int.TryParse(tail, out int num))
                return $"Level {num}";
        }

        return n.Length > 0 ? n : "Level";
    }
}
