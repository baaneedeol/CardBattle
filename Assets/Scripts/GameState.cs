using System.Collections.Generic;
using UnityEngine;

public class GameState : MonoBehaviour
{
    // 0 = human player, 1 = AI opponent.
    public int currentTurn = 0;

    public List<Player> players = new List<Player>();

    public bool gameOver = false;

    [Header("Deck Management")]
    public List<Card> drawPile = new List<Card>();
    public List<Card> discardPile = new List<Card>();
    public List<Card> playerDiscardPile = new List<Card>();
    public List<Card> aiDiscardPile = new List<Card>();

    [Header("AI History")]
    // Most recent human actions are at the end.
    public List<AbilityType> recentPlayerActions =
        new List<AbilityType>();

    public Player GetCurrentPlayer()
    {
        if (players == null ||
            players.Count < 2 ||
            currentTurn < 0 ||
            currentTurn >= players.Count)
        {
            return null;
        }

        return players[currentTurn];
    }

    public Player GetPlayer(int index)
    {
        if (players == null ||
            index < 0 ||
            index >= players.Count)
        {
            return null;
        }

        return players[index];
    }

    public void RecordPlayerAction(AbilityType ability)
    {
        recentPlayerActions.Add(ability);

        // Keep enough history for the AI's consecutive-action rules.
        while (recentPlayerActions.Count > 2)
        {
            recentPlayerActions.RemoveAt(0);
        }
    }

    public bool DidPlayerUseAbilityLastTwoTurns(
        AbilityType ability)
    {
        return recentPlayerActions.Count >= 2 &&
               recentPlayerActions[
                   recentPlayerActions.Count - 1] == ability &&
               recentPlayerActions[
                   recentPlayerActions.Count - 2] == ability;
    }

    public void UpdateState()
    {
        if (players == null)
            return;

        foreach (Player player in players)
        {
            if (player == null)
                continue;

            player.maxHealth = Mathf.Max(1, player.maxHealth);

            player.health = Mathf.Clamp(
                player.health,
                0,
                player.maxHealth
            );

            player.shield = Mathf.Max(0, player.shield);

            if (player.hand == null)
            {
                player.hand = new List<Card>();
            }
        }

        if (drawPile == null)
            drawPile = new List<Card>();

        if (discardPile == null)
            discardPile = new List<Card>();

        if (recentPlayerActions == null)
        {
            recentPlayerActions = new List<AbilityType>();
        }
    }
}