using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AttackTypeSelector : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject selectorPanel;
    [SerializeField] private Button swordButton;
    [SerializeField] private Button bowButton;
    [SerializeField] private Button magicButton;
    
    [Header("References")]
    [SerializeField] private BattleSystem battleSystem;
    [SerializeField] private UIManager uiManager;

    public void Initialize(BattleSystem battleSystem, UIManager uiManager)
    {
        this.battleSystem = battleSystem;
        this.uiManager = uiManager;
        Debug.Log("AttackTypeSelector initialized with references");
    }

    private void Awake()
    {
        if (swordButton != null)
            swordButton.onClick.AddListener(() => OnAttackTypeSelected(AttackType.Sword));
            
        if (bowButton != null)
            bowButton.onClick.AddListener(() => OnAttackTypeSelected(AttackType.Bow));
            
        if (magicButton != null)
            magicButton.onClick.AddListener(() => OnAttackTypeSelected(AttackType.Magic));
            
        // Скрываем панель при старте
        if (selectorPanel != null)
            selectorPanel.SetActive(false);
    }

    public void ShowSelector()
    {
        if (selectorPanel != null)
            selectorPanel.SetActive(true);
    }

    public void HideSelector()
    {
        if (selectorPanel != null)
            selectorPanel.SetActive(false);
    }

    private void OnAttackTypeSelected(AttackType type)
    {
        if (battleSystem != null && battleSystem.GetPlayerSquad().HasActiveCharacter())
        {
            Character activeCharacter = battleSystem.GetPlayerSquad().GetActiveCharacter();
            activeCharacter.SetAttackType(type);
            
            // Скрываем панель выбора
            HideSelector();
            
            // Уведомляем BattleSystem о выборе оружия
            battleSystem.OnWeaponSelected();
            
            // Активируем выбор цели
            if (uiManager != null)
            {
                uiManager.EnableEnemySelection(true);
                uiManager.ShowMessage($"Выбран тип атаки: {GetAttackTypeName(type)}");
            }
        }
    }

    private string GetAttackTypeName(AttackType type)
    {
        switch (type)
        {
            case AttackType.Sword:
                return "Меч";
            case AttackType.Bow:
                return "Лук";
            case AttackType.Magic:
                return "Магия";
            default:
                return "Неизвестный тип";
        }
    }
} 