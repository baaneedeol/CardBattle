using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Player
{
    public int health = 20;
    public int maxHealth = 20;
    public int shield = 0;

    public List<Card> hand = new List<Card>();

    public void playCard(Card card)
    {
        if (card != null && hand != null)
        {
            hand.Remove(card);
        }
    }

    // Returns actual HP damage after shield absorption.
    public int takeDamage(int amount)
    {
        amount = Mathf.Max(0, amount);

        // Shield absorbs damage before health.
        int absorbed = Mathf.Min(Mathf.Max(0, shield), amount);

        shield -= absorbed;
        amount -= absorbed;

        // Only remaining damage affects health.
        int actualDamage = Mathf.Min(
            Mathf.Max(0, health),
            amount
        );

        health = Mathf.Max(0, health - actualDamage);

        return actualDamage;
    }

    // Returns actual HP restored, limited by maximum health.
    public int heal(int amount)
    {
        amount = Mathf.Max(0, amount);

        int oldHealth = health;

        health = Mathf.Clamp(
            health + amount,
            0,
            maxHealth
        );

        return health - oldHealth;
    }

    // Adds a nonnegative amount of shield.
    // GameManager enforces the maximum shield of 8.
    public void addShield(int amount)
    {
        shield = Mathf.Max(0, shield + Mathf.Max(0, amount));
    }

    // Removes all remaining shield.
    public void clearShield()
    {
        shield = 0;
    }

    public bool HasCard(Suit suit, int rank)
    {
        if (hand == null)
            return false;

        foreach (Card card in hand)
        {
            if (card != null &&
                card.suit == suit &&
                card.rank == rank)
            {
                return true;
            }
        }

        return false;
    }
}