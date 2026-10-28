
using System.Collections;
using UnityEngine;

public class MixedRealityUITest : MonoBehaviour
{
    [SerializeField] private Mixed_Reality_UI battleUI;
    [SerializeField] private float testDuration = 3f;

    private void Start()
    {
        if (battleUI == null)
        {
            Debug.LogError(
                "MixedRealityUITest: Assign Mixed_Reality_UI in the Inspector."
            );
            enabled = false;
            return;
        }

        StartCoroutine(RunAllTests());
    }

    private IEnumerator RunAllTests()
    {
        Debug.Log("Starting automatic UI test sequence.");

        // 1. Start screen and welcome message
        battleUI.startGame();
        battleUI.showWelcomeMessage();
        yield return new WaitForSeconds(testDuration);

        // 2. Recognized attack card
        battleUI.onCardRecognized("attack", 5);
        yield return new WaitForSeconds(testDuration);

        // 3. Attack effect
        battleUI.showAttackMessage(5);
        yield return new WaitForSeconds(testDuration);

        // 4. Healing effect
        battleUI.showHealMessage(5);
        yield return new WaitForSeconds(testDuration);

        // 5. Shield effect
        battleUI.showShieldMessage(5);
        yield return new WaitForSeconds(testDuration);

        // 6. Drain effect
        battleUI.showDrainMessage(3);
        yield return new WaitForSeconds(testDuration);

        // 7. Player turn
        battleUI.showPlayerTurnMessage();
        yield return new WaitForSeconds(testDuration);

        // 8. Opponent turn
        battleUI.showOpponentTurnMessage();
        yield return new WaitForSeconds(testDuration);

        // 9. Health bar update
        battleUI.updateUI(12, 3, 8, 2, true, false);
        battleUI.showMessage(
            "Health update test: Player 12 HP, Opponent 8 HP",
            testDuration
        );
        yield return new WaitForSeconds(testDuration);

        // 10. Blocked attack
        battleUI.showBlockedAttackMessage();
        yield return new WaitForSeconds(testDuration);

        // 11. Card removed
        battleUI.showCardRemovedMessage();
        yield return new WaitForSeconds(testDuration);

        // 12. Player wins
        battleUI.showGameOver(true);
        yield return new WaitForSeconds(testDuration);

        // 13. Player loses
        battleUI.showGameOver(false);
        yield return new WaitForSeconds(testDuration);

        Debug.Log("Automatic UI test sequence complete.");
    }
}
