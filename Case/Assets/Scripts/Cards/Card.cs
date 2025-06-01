using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class CardEffect
{
    public float HealthPercentageModifier = 0f;
    public int AttackModifier = 0;
    public int ArmorModifier = 0;
    public float EnergyModifier = 0f;
    
    public string GetDescription()
    {
        string description = "";
        
        if (HealthPercentageModifier > 0)
        {
            description += $"+{HealthPercentageModifier}% здоровья\n";
        }
        else if (HealthPercentageModifier < 0)
        {
            description += $"{HealthPercentageModifier}% здоровья\n";
        }
        
        if (AttackModifier > 0)
        {
            description += $"+{AttackModifier} атаки\n";
        }
        else if (AttackModifier < 0)
        {
            description += $"{AttackModifier} атаки\n";
        }
        
        if (ArmorModifier > 0)
        {
            description += $"+{ArmorModifier} брони\n";
        }
        else if (ArmorModifier < 0)
        {
            description += $"{ArmorModifier} брони\n";
        }
        
        if (EnergyModifier > 0)
        {
            description += $"+{EnergyModifier} энергии\n";
        }
        else if (EnergyModifier < 0)
        {
            description += $"{EnergyModifier} энергии\n";
        }
        
        return description;
    }
}

[RequireComponent(typeof(Button))]
public class Card : MonoBehaviour
{
    [Header("Card Details")]
    [SerializeField] private string cardName;
    [SerializeField] private string description;
    [SerializeField] private CardEffect effect;
    [SerializeField] private Sprite cardImage;
    [SerializeField] private Color cardColor = Color.white;
    
    [Header("UI References")]
    [SerializeField] private Image cardBackground;
    [SerializeField] private Image cardArtwork;
    [SerializeField] private TextMeshProUGUI cardNameText;
    [SerializeField] private TextMeshProUGUI cardDescriptionText;
    private Button cardButton;
    
    private Color originalColor;
    private Vector3 originalScale;
    private RectTransform rectTransform;
    private CardManager cardManager;
    
    public string CardName => cardName;
    public CardEffect Effect => effect;
    
    private void Awake()
    {
        Debug.Log($"Card {cardName} Awake - Initializing components");
        
        cardButton = GetComponent<Button>();
        if (cardButton != null)
        {
            cardButton.onClick.AddListener(OnCardClicked);
            Debug.Log($"Card {cardName} - Button component found and click listener added");
        }
        else
        {
            Debug.LogError($"Card {cardName} - Button component not found!");
        }

        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;
        if (cardBackground != null)
        {
            originalColor = cardBackground.color;
        }
        
        // Verify all required components
        if (cardBackground == null) Debug.LogError($"Card {cardName} - Missing cardBackground reference");
        if (cardArtwork == null) Debug.LogError($"Card {cardName} - Missing cardArtwork reference");
        if (cardNameText == null) Debug.LogError($"Card {cardName} - Missing cardNameText reference");
        if (cardDescriptionText == null) Debug.LogError($"Card {cardName} - Missing cardDescriptionText reference");
    }
    
    private void OnEnable()
    {
        Debug.Log($"Card {cardName} OnEnable - Updating visuals");
        UpdateCardVisuals();
    }
    
    private void OnDisable()
    {
        if (cardButton != null)
        {
            cardButton.onClick.RemoveListener(OnCardClicked);
            Debug.Log($"Card {cardName} OnDisable - Removed click listener");
        }
    }
    
    public void Initialize(CardManager manager, CardData cardData)
    {
        Initialize(manager, cardData.CardName, cardData.Description, cardData.Effect, cardData.CardImage, cardData.CardColor);
    }
    
    public void Initialize(CardManager manager, string name, string description, CardEffect effect, Sprite image, Color color)
    {
        this.cardManager = manager;
        this.cardName = name;
        this.description = description;
        this.effect = effect;
        this.cardImage = image;
        this.cardColor = color;
        
        UpdateCardVisuals();
    }
    
    private void UpdateCardVisuals()
    {
        if (cardNameText != null) cardNameText.text = cardName;
        if (cardDescriptionText != null) cardDescriptionText.text = description;
        if (cardBackground != null) cardBackground.color = cardColor;
        if (cardArtwork != null && cardImage != null) cardArtwork.sprite = cardImage;
    }
    
    private void OnCardClicked()
    {
        Debug.Log($"Card {cardName} clicked");
        
        if (CardManager.Instance != null)
        {
            CardManager.Instance.OnCardSelected(this);
        }
        else
        {
            Debug.LogError($"Card {cardName} - CardManager.Instance is null!");
        }
    }

    public CardEffect GetEffect()
    {
        return effect;
    }

    public string GetCardName()
    {
        return cardName;
    }
}

[System.Serializable]
public class CardData
{
    public string CardName;
    public string Description;
    public CardEffect Effect;
    public Sprite CardImage;
    public Color CardColor = Color.white;
} 