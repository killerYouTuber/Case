using UnityEngine;
using TMPro;

public class CharacterStatsDisplay : MonoBehaviour
{
    [Header("Character Reference")]
    [SerializeField] private Character character;

    [Header("UI Text Components")]
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI armorText;
    [SerializeField] private TextMeshProUGUI energyText;

    private void OnEnable()
    {
        if (character != null)
        {
            // Subscribe to character events
            character.OnHealthChanged += UpdateHealthDisplay;
            character.OnEnergyChanged += UpdateEnergyDisplay;
            character.OnArmorChanged += UpdateArmorDisplay;

            // Initial update
            UpdateAllStats();
        }
        else
        {
            Debug.LogError("Character reference is missing in CharacterStatsDisplay!");
        }
    }

    private void OnDisable()
    {
        if (character != null)
        {
            // Unsubscribe from character events
            character.OnHealthChanged -= UpdateHealthDisplay;
            character.OnEnergyChanged -= UpdateEnergyDisplay;
            character.OnArmorChanged -= UpdateArmorDisplay;
        }
    }

    public void SetCharacter(Character newCharacter)
    {
        if (character != null)
        {
            // Unsubscribe from old character events
            character.OnHealthChanged -= UpdateHealthDisplay;
            character.OnEnergyChanged -= UpdateEnergyDisplay;
            character.OnArmorChanged -= UpdateArmorDisplay;
        }

        character = newCharacter;

        if (character != null)
        {
            // Subscribe to new character events
            character.OnHealthChanged += UpdateHealthDisplay;
            character.OnEnergyChanged += UpdateEnergyDisplay;
            character.OnArmorChanged += UpdateArmorDisplay;

            // Update displays
            UpdateAllStats();
        }
    }

    private void Update()
    {
        // Update attack display every frame since we don't have a specific event for it
        UpdateAttackDisplay();
    }

    private void UpdateAllStats()
    {
        UpdateHealthDisplay(character.GetCurrentHealth(), character.GetMaxHealth());
        UpdateEnergyDisplay(character.GetCurrentEnergy(), character.GetMaxEnergy());
        UpdateArmorDisplay(character.GetCurrentArmor());
        UpdateAttackDisplay();
    }

    private void UpdateHealthDisplay(int current, int max)
    {
        if (healthText != null)
        {
            healthText.text = $"Здоровье {current}/{max}";
        }
    }

    private void UpdateEnergyDisplay(float current, float max)
    {
        if (energyText != null)
        {
            energyText.text = $"Энергия {Mathf.Floor(current)}/{max}";
        }
    }

    private void UpdateArmorDisplay(int armor)
    {
        if (armorText != null)
        {
            armorText.text = $"Броня {armor}";
        }
    }

    private void UpdateAttackDisplay()
    {
        if (attackText != null && character != null)
        {
            attackText.text = $"Атака {character.GetAttackPower()}";
        }
    }
} 