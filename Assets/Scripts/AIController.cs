using System.Collections.Generic;
using UnityEngine;

public class AIController : MonoBehaviour
{
    public Card SelectCard(GameState state)
    {
        if (state == null || state.players == null ||
            state.players.Count < 2)
        {
            return null;
        }

        Player ai = state.players[1];
        Player opponent = state.players[0];

        if (ai == null || opponent == null ||
            ai.hand == null || ai.hand.Count == 0)
        {
            return null;
        }

        List<Card> hand = ai.hand;

        // Priority 1: Win immediately.
        Card winningCard = GetBestCard(hand, card =>
        {
            if (card.ability != AbilityType.Attack &&
                card.ability != AbilityType.Drain)
            {
                return false;
            }

            int effectiveDamage = Mathf.Max(
                0,
                card.rank - opponent.shield
            );

            return effectiveDamage >= opponent.health;
        });

        if (winningCard != null)
            return winningCard;

        // Priority 2: Heal when health is low.
        if (ai.health <= 8 && ai.health < ai.maxHealth)
        {
            Card heal = GetBestCard(hand, card =>
                card.ability == AbilityType.Heal);

            if (heal != null)
                return heal;
        }

        // Priority 3: If low on health and no heal is available,
        // use the strongest shield.
        if (ai.health <= 8)
        {
            Card shield = GetBestCard(hand, card =>
                card.ability == AbilityType.Shield);

            if (shield != null)
                return shield;
        }

        // Priority 4: Drain can attack and restore HP.
        if (ai.health < ai.maxHealth)
        {
            Card drain = GetBestCard(hand, card =>
                card.ability == AbilityType.Drain);

            if (drain != null)
                return drain;
        }

        // Priority 5: Finish off a low-health opponent.
        if (opponent.health <= 5)
        {
            Card attack = GetBestCard(hand, card =>
                card.ability == AbilityType.Attack ||
                card.ability == AbilityType.Drain);

            if (attack != null)
                return attack;
        }

        // Priority 6: Counter consecutive player attacks.
        if (state.DidPlayerUseAbilityLastTwoTurns(
                AbilityType.Attack))
        {
            Card shield = GetBestCard(hand, card =>
                card.ability == AbilityType.Shield);

            if (shield != null)
                return shield;
        }

        // Priority 7: Counter consecutive player heals.
        if (state.DidPlayerUseAbilityLastTwoTurns(
                AbilityType.Heal))
        {
            Card attack = GetBestCard(hand, card =>
                card.ability == AbilityType.Attack ||
                card.ability == AbilityType.Drain);

            if (attack != null)
                return attack;
        }

        // Priority 8: Defensive protection when threatened.
        bool opponentHasAttack =
            opponent.hand != null &&
            opponent.hand.Exists(card =>
                card != null &&
                (card.ability == AbilityType.Attack ||
                 card.ability == AbilityType.Drain));

        bool needsProtection =
            ai.shield == 0 &&
            (ai.health <= 12 || opponentHasAttack);

        if (needsProtection)
        {
            Card shield = GetBestCard(hand, card =>
                card.ability == AbilityType.Shield);

            if (shield != null)
                return shield;
        }

        // Priority 9: Strongest attack or drain.
        Card strongestAttack = GetBestCard(hand, card =>
            card.ability == AbilityType.Attack ||
            card.ability == AbilityType.Drain);

        if (strongestAttack != null)
            return strongestAttack;

        // Priority 10: Strongest useful remaining card.
        Card strongestUseful = GetBestCard(hand, card =>
        {
            if (!card.IsValid())
                return false;

            if (card.ability == AbilityType.Heal)
                return ai.health < ai.maxHealth;

            if (card.ability == AbilityType.Shield)
                return ai.shield == 0;

            return true;
        });

        return strongestUseful;
    }

    public List<Card> GetLegalCards(GameState state)
    {
        List<Card> legalCards = new List<Card>();

        if (state == null ||
            state.players == null ||
            state.players.Count < 2)
        {
            return legalCards;
        }

        Player ai = state.players[1];

        if (ai == null || ai.hand == null)
            return legalCards;

        foreach (Card card in ai.hand)
        {
            if (card != null && card.IsValid())
            {
                legalCards.Add(card);
            }
        }

        return legalCards;
    }

    public Card EvaluateCards(GameState state)
    {
        return SelectCard(state);
    }

    private Card GetBestCard(
        List<Card> hand,
        System.Predicate<Card> condition)
    {
        Card best = null;

        foreach (Card card in hand)
        {
            if (card == null ||
                !card.IsValid() ||
                !condition(card))
            {
                continue;
            }

            if (best == null || card.rank > best.rank)
            {
                best = card;
            }
        }

        return best;
    }
}