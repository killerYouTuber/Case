using UnityEngine;

public class BattleStatistics
{
    public int TotalTurns { get; private set; }
    public int DamageDealt { get; private set; }
    public int DamageReceived { get; private set; }

    public void IncrementTurn()
    {
        TotalTurns++;
    }

    public void AddDamageDealt(int damage)
    {
        DamageDealt += damage;
    }

    public void AddDamageReceived(int damage)
    {
        DamageReceived += damage;
    }

    public void Reset()
    {
        TotalTurns = 0;
        DamageDealt = 0;
        DamageReceived = 0;
    }
} 