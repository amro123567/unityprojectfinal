using UnityEngine;
using UnityEngine.UI;

public sealed class LiveHud : MonoBehaviour
{
    Text readout;

    public void Bind(Text label)
    {
        readout = label;
    }

    void LateUpdate()
    {
        if (readout == null)
            return;

        HeroKnight hero = FindObjectOfType<HeroKnight>();

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
