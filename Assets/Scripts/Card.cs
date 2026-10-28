using System;
using UnityEngine;

public enum Suit
{
    Spades,
    Hearts,
    Diamonds,
    Clubs
}

public enum AbilityType
{
    Attack,
    Heal,
    Drain,
    Shield
}

[Serializable]
public class Card
{
    public Suit suit;
    public int rank;
    public AbilityType ability;

    public Card()
    {
    }

    public Card(Suit suit, int rank, AbilityType ability)
    {
        this.suit = suit;
        this.rank = rank;

        // The suit determines the ability.
        this.ability = GetAbilityForSuit(suit);
    }

    public static AbilityType GetAbilityForSuit(Suit suit)
    {
        switch (suit)
        {
            case Suit.Spades:
                return AbilityType.Attack;

            case Suit.Hearts:
                return AbilityType.Heal;

            case Suit.Diamonds:
                return AbilityType.Drain;

            case Suit.Clubs:
                return AbilityType.Shield;

            default:
                return AbilityType.Attack;
        }
    }

    public bool IsValid()
    {
        bool validRank =
            rank == 2 || rank == 4 ||
            rank == 6 || rank == 8;

        bool validSuit =
            Enum.IsDefined(typeof(Suit), suit);

        return validRank &&
               validSuit &&
               ability == GetAbilityForSuit(suit);
    }

    public int getStrength()
    {
        return rank;
    }

    public override string ToString()
    {
        return $"{suit} {rank}";
    }
}