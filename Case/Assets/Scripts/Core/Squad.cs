using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Squad : MonoBehaviour
{
    [SerializeField] private Team squadTeam;
    [SerializeField] private List<Character> squadMembers = new List<Character>();
    [SerializeField] private Transform squadFormationCenter;

    private bool hasActiveCharacter = false;
    private Character activeCharacter;

    private void Awake()
    {
        InitializeSquad();
    }

    private void InitializeSquad()
    {
        foreach (Character character in squadMembers)
        {
            if (character != null)
            {
                character.Initialize(squadTeam);
            }
        }

        // Автоматически выбираем первого живого персонажа как активного
        Character firstAliveCharacter = squadMembers.FirstOrDefault(c => c != null && c.IsAlive());
        if (firstAliveCharacter != null)
        {
            SetActiveCharacter(firstAliveCharacter);
        }
        else
        {
            Debug.LogWarning($"No alive characters found in {squadTeam} squad!");
        }
    }

    public Team GetTeam()
    {
        return squadTeam;
    }

    public List<Character> GetAliveMembers()
    {
        return squadMembers.Where(member => member != null && member.IsAlive()).ToList();
    }

    public int GetTotalHealth()
    {
        int totalHealth = 0;
        foreach (Character member in squadMembers)
        {
            if (member != null && member.IsAlive())
            {
                totalHealth += member.GetCurrentHealth();
            }
        }
        return totalHealth;
    }

    public int GetMembersCount()
    {
        return squadMembers.Count;
    }

    public int GetAliveMembersCount()
    {
        return GetAliveMembers().Count;
    }

    public bool HasActiveCharacter()
    {
        return hasActiveCharacter && activeCharacter != null && activeCharacter.IsAlive();
    }

    public Character GetActiveCharacter()
    {
        return activeCharacter;
    }

    public void SetActiveCharacter(int index)
    {
        if (index >= 0 && index < squadMembers.Count && squadMembers[index] != null && squadMembers[index].IsAlive())
        {
            activeCharacter = squadMembers[index];
            hasActiveCharacter = true;
        }
    }

    public void SetActiveCharacter(Character character)
    {
        if (character != null && character.IsAlive() && squadMembers.Contains(character))
        {
            activeCharacter = character;
            hasActiveCharacter = true;
        }
    }

    public void ClearActiveCharacter()
    {
        activeCharacter = null;
        hasActiveCharacter = false;
    }

    public Transform GetFormationCenter()
    {
        return squadFormationCenter;
    }

    public void ApplyCardEffect(CardEffect effect)
    {
        foreach (Character member in GetAliveMembers())
        {
            if (effect.HealthPercentageModifier != 0)
            {
                member.HealPercentage(effect.HealthPercentageModifier);
            }

            if (effect.AttackModifier != 0)
            {
                member.ModifyAttackPower(effect.AttackModifier);
            }

            if (effect.ArmorModifier != 0)
            {
                member.ModifyArmor(effect.ArmorModifier);
            }

            if (effect.EnergyModifier != 0)
            {
                member.IncreaseEnergy(effect.EnergyModifier);
            }
        }
    }

    public bool IsSquadDefeated()
    {
        return GetAliveMembersCount() == 0;
    }
} 