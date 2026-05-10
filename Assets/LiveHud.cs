using UnityEngine;
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

        readout.text = $"{current}/{cap}";
    }
}
