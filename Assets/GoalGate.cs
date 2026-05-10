using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GoalGate : MonoBehaviour
{
    bool grantDoubleJump;
    int destinationBuildIndex;

    public void Configure(int activeBuildIndex)
    {
        Scene scene = SceneManager.GetActiveScene();
        string sn = scene.name;

        bool isLevel2 =
            activeBuildIndex == 2 ||
            sn.IndexOf("Level2", System.StringComparison.OrdinalIgnoreCase) >= 0;

        if (isLevel2)
        {
            grantDoubleJump = false;
            destinationBuildIndex = 0;
        }
        else
        {
            grantDoubleJump = true;
            destinationBuildIndex = 2;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        HeroKnight hero = other.GetComponentInParent<HeroKnight>();

        SaveSystem saver = SaveSystem.Instance;

        if (saver == null || hero == null)
            return;

        if (grantDoubleJump)
            saver.UnlockDoubleJumpPersist();

        int snapshotLevel = destinationBuildIndex == 0 ? 1 : destinationBuildIndex;

        saver.SaveGame(
            hero.transform,
            Mathf.Max(1f, hero.GetHealthCurrent()),
            snapshotLevel,
            saver.GetData().score,
            saver.GetData().hasDoubleJump);

        Time.timeScale = 1f;

        SceneLoader loader = SceneLoader.Instance;

        if (loader != null)
            loader.LoadScene(destinationBuildIndex);
    }
}
