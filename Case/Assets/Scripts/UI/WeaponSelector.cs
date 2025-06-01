using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponSelector : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button swordButton;
    [SerializeField] private Button bowButton;
    [SerializeField] private Button magicButton;
    
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private BattleSystem battleSystem;
    
    private Character currentCharacter;

    private void Start()
    {
        if (swordButton != null)
            swordButton.onClick.AddListener(() => OnWeaponSelected(AttackType.Sword));
            
        if (bowButton != null)
            bowButton.onClick.AddListener(() => OnWeaponSelected(AttackType.Bow));
            
        if (magicButton != null)
            magicButton.onClick.AddListener(() => OnWeaponSelected(AttackType.Magic));
            
        // Скрываем панель при старте
        gameObject.SetActive(false);
    }

    public void Show(Character character)
    {
        currentCharacter = character;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        currentCharacter = null;
    }

    private void OnWeaponSelected(AttackType weaponType)
    {
        if (currentCharacter != null)
        {
            currentCharacter.SetAttackType(weaponType);
            
            // Показываем радиус атаки после выбора оружия
            gridManager.StartAttackMode(currentCharacter);
        }
        
        Hide();
    }
} 