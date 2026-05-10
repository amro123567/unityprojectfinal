using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class LiveHud : MonoBehaviour
{
    Text readout;
    Image healthFill;
    HeroKnight trackedHero;

    public void Bind(Text label, Image trackedHealthFill = null)
    {
        readout = label;
        healthFill = trackedHealthFill;
        trackedHero = null;
    }

    void LateUpdate()
    {
        if (readout == null && healthFill == null)
            return;

        if (trackedHero == null)
            trackedHero = Object.FindFirstObjectByType<HeroKnight>();

        HeroKnight hero = trackedHero;

        if (hero == null)
        {
            if (readout != null)
                readout.text = string.Empty;

            if (healthFill != null)
                healthFill.fillAmount = 0f;

            return;
        }

        float pct = Mathf.Clamp01(hero.GetHealthPercent());

        if (healthFill != null)
            healthFill.fillAmount = pct;

        if (readout == null)
            return;

        int cap = Mathf.Max(1, Mathf.RoundToInt(hero.GetHealthMax()));

        int current = Mathf.Clamp(Mathf.RoundToInt(hero.GetHealthCurrent()), 0, cap);

        Scene s = SceneManager.GetActiveScene();
        readout.text = $"{DescribeLevelLine(s)}\nHP: {current} / {cap}";
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
