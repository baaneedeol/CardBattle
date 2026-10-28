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

    // Returns the amount of HP damage after shield absorption.
    public int takeDamage(int amount)
    {
        amount = Mathf.Max(0, amount);

        int absorbed = Mathf.Min(shield, amount);

        shield -= absorbed;
        amount -= absorbed;

        int actualDamage = Mathf.Min(health, amount);

        health = Mathf.Max(0, health - amount);

        return actualDamage;
    }

    // Returns the actual amount of HP restored.
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

    public void addShield(int amount)
    {
        shield += Mathf.Max(0, amount);
    }

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