using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;

public class CardManager : MonoBehaviour
{
    public static CardManager Instance { get; private set; }

    [Header("Card References")]
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform playerCardContainer;
    [SerializeField] private Transform enemyCardContainer;
    
    [Header("Card Settings")]
    [SerializeField] private List<CardData> cardDataPool = new List<CardData>();
    [SerializeField] private Color playerCardColor = new Color(0.2f, 0.5f, 1f);
    [SerializeField] private Color enemyCardColor = new Color(1f, 0.2f, 0.2f);
    
    [Header("Default Card Images")]
    [SerializeField] private Sprite attackCardImage;
    [SerializeField] private Sprite defenseCardImage;
    [SerializeField] private Sprite healCardImage;
    [SerializeField] private Sprite balancedCardImage;
    
    [Header("References")]
    [SerializeField] private UIManager uiManager;
    [SerializeField] private Squad playerSquad;
    [SerializeField] private Squad enemySquad;
    
    private List<Card> playerCards = new List<Card>();
    private List<Card> enemyCards = new List<Card>();
    private bool canUseCards = true;

    private void Awake()
    {
        Debug.Log("CardManager Awake - Initializing");
        
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("CardManager instance set");
        }
        else
        {
            Debug.LogWarning("Multiple CardManager instances detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        // Verify required components
        if (cardPrefab == null || playerCardContainer == null || enemyCardContainer == null)
        {
            Debug.LogError("Required references are missing in CardManager!");
            return;
        }

        if (playerSquad == null)
        {
            Debug.LogError("PlayerSquad reference is missing in CardManager!");
            return;
        }

        if (enemySquad == null)
        {
            Debug.LogError("EnemySquad reference is missing in CardManager!");
            return;
        }

        // Generate default cards if pool is empty
        if (cardDataPool.Count == 0)
        {
            Debug.Log("CardManager - Generating default card pool");
            GenerateDefaultCardPool();
        }

        InitializeCards();
    }
    
    private void Start()
    {
        Debug.Log("=== ИНИЦИАЛИЗАЦИЯ CARD MANAGER ===");
        
        // Проверяем и находим необходимые компоненты
        if (uiManager == null)
            uiManager = FindAnyObjectByType<UIManager>();
        if (playerSquad == null)
            playerSquad = FindAnyObjectByType<Squad>();
        if (enemySquad == null)
            enemySquad = FindAnyObjectByType<Squad>();

        // Проверяем наличие карт в пуле
        if (cardDataPool.Count == 0)
        {
            Debug.Log("Генерируем стандартный набор карт");
            GenerateDefaultCardPool();
        }

        // Инициализируем карты
        InitializeCards();
        
        Debug.Log("=== ЗАВЕРШЕНИЕ ИНИЦИАЛИЗАЦИИ CARD MANAGER ===");
    }
    
    private void InitializeCards()
    {
        Debug.Log("Начало инициализации карт");
        
        // Очищаем существующие карты
        ClearAllCards();
        
        // Проверяем необходимые ссылки
        if (cardPrefab == null || playerCardContainer == null || enemyCardContainer == null)
        {
            Debug.LogError("Отсутствуют необходимые ссылки для создания карт!");
            return;
        }

        if (cardDataPool.Count == 0)
        {
            Debug.LogWarning("Пул карт пуст! Генерируем стандартный набор...");
            GenerateDefaultCardPool();
        }

        // Генерируем начальные карты для игрока
        GeneratePlayerCards();
        
        Debug.Log($"Создано {playerCards.Count} карт игрока");
        Debug.Log("Завершение инициализации карт");
    }
    
    private void GenerateDefaultCardPool()
    {
        // Attack focused cards
        CardData attackCard1 = new CardData
        {
            CardName = "Яростный натиск",
            Description = "Увеличивает атаку на 5, но снижает броню на 2",
            Effect = new CardEffect { AttackModifier = 5, ArmorModifier = -2 },
            CardImage = attackCardImage,
            CardColor = new Color(0.9f, 0.3f, 0.3f)
        };
        cardDataPool.Add(attackCard1);
        
        CardData attackCard2 = new CardData
        {
            CardName = "Боевой клич",
            Description = "Увеличивает атаку на 3 и энергию на 20",
            Effect = new CardEffect { AttackModifier = 3, EnergyModifier = 20f },
            CardImage = attackCardImage,
            CardColor = new Color(0.9f, 0.4f, 0.1f)
        };
        cardDataPool.Add(attackCard2);
        
        // Defense focused cards
        CardData defenseCard1 = new CardData
        {
            CardName = "Каменная стена",
            Description = "Увеличивает броню на 5, но снижает атаку на 2",
            Effect = new CardEffect { ArmorModifier = 5, AttackModifier = -2 },
            CardImage = defenseCardImage,
            CardColor = new Color(0.3f, 0.6f, 0.9f)
        };
        cardDataPool.Add(defenseCard1);
        
        CardData defenseCard2 = new CardData
        {
            CardName = "Твердыня",
            Description = "Увеличивает броню на 3 и здоровье на 10%",
            Effect = new CardEffect { ArmorModifier = 3, HealthPercentageModifier = 10f },
            CardImage = defenseCardImage,
            CardColor = new Color(0.4f, 0.5f, 0.9f)
        };
        cardDataPool.Add(defenseCard2);
        
        // Healing cards
        CardData healCard1 = new CardData
        {
            CardName = "Исцеление",
            Description = "Восстанавливает 15% здоровья всем членам отряда",
            Effect = new CardEffect { HealthPercentageModifier = 15f },
            CardImage = healCardImage,
            CardColor = new Color(0.3f, 0.9f, 0.4f)
        };
        cardDataPool.Add(healCard1);
        
        CardData healCard2 = new CardData
        {
            CardName = "Регенерация",
            Description = "Восстанавливает 7% здоровья и увеличивает броню на 2",
            Effect = new CardEffect { HealthPercentageModifier = 7f, ArmorModifier = 2 },
            CardImage = healCardImage,
            CardColor = new Color(0.5f, 0.9f, 0.5f)
        };
        cardDataPool.Add(healCard2);
        
        // Balanced cards
        CardData balancedCard1 = new CardData
        {
            CardName = "Баланс сил",
            Description = "Увеличивает атаку и броню на 2",
            Effect = new CardEffect { AttackModifier = 2, ArmorModifier = 2 },
            CardImage = balancedCardImage,
            CardColor = new Color(0.7f, 0.7f, 0.2f)
        };
        cardDataPool.Add(balancedCard1);
        
        CardData balancedCard2 = new CardData
        {
            CardName = "Гармония",
            Description = "Восстанавливает 5% здоровья и увеличивает энергию на 15",
            Effect = new CardEffect { HealthPercentageModifier = 5f, EnergyModifier = 15f },
            CardImage = balancedCardImage,
            CardColor = new Color(0.8f, 0.6f, 0.2f)
        };
        cardDataPool.Add(balancedCard2);
        
        // Super meter focused
        CardData superCard = new CardData
        {
            CardName = "Заряд энергии",
            Description = "Увеличивает энергию на 30, но снижает броню на 1",
            Effect = new CardEffect { EnergyModifier = 30f, ArmorModifier = -1 },
            CardImage = balancedCardImage,
            CardColor = new Color(0.9f, 0.2f, 0.9f)
        };
        cardDataPool.Add(superCard);
        
        // Risk cards
        CardData riskCard = new CardData
        {
            CardName = "Отчаянный рывок",
            Description = "Увеличивает атаку на 7, но снижает здоровье на 5%",
            Effect = new CardEffect { AttackModifier = 7, HealthPercentageModifier = -5f },
            CardImage = attackCardImage,
            CardColor = new Color(1f, 0.3f, 0.1f)
        };
        cardDataPool.Add(riskCard);
    }
    
    private Card CreateCard(CardData cardData, bool isPlayerCard)
    {
        Debug.Log($"CardManager CreateCard - Creating {(isPlayerCard ? "player" : "enemy")} card");
        
        var cardObject = Instantiate(cardPrefab, isPlayerCard ? playerCardContainer : enemyCardContainer);
        var card = cardObject.GetComponent<Card>();
        
        if (card != null)
        {
            // Set card properties from cardData
            card.Initialize(this, cardData);
            
            // Add to appropriate list
            if (isPlayerCard)
            {
                playerCards.Add(card);
            }
            else
            {
                enemyCards.Add(card);
            }
            
            Debug.Log($"CardManager CreateCard - Card created successfully");
        }
        else
        {
            Debug.LogError("CardManager CreateCard - Failed to get Card component from instantiated prefab!");
        }
        
        return card;
    }

    public void StartNewTurn()
    {
        canUseCards = true;
        
        // Генерируем карты для обоих сторон в начале хода игрока
        GeneratePlayerCards();
        GenerateEnemyCards();
    }

    public void GeneratePlayerCards()
    {
        ClearPlayerCards();  // Очищаем старые карты игрока
        
        if (!canUseCards)
        {
            Debug.Log("Cannot generate cards - cards already used this turn");
            return;
        }

        if (cardPrefab == null || playerCardContainer == null)
        {
            Debug.LogError("Required references are missing in CardManager!");
            return;
        }

        // Check if card pool is empty and generate default cards if needed
        if (cardDataPool.Count == 0)
        {
            Debug.LogWarning("Card pool is empty! Generating default cards...");
            GenerateDefaultCardPool();
            
            if (cardDataPool.Count == 0)
            {
                Debug.LogError("Failed to generate card pool!");
                return;
            }
        }
        
        // Generate player cards
        List<CardData> availableCards = new List<CardData>(cardDataPool);
        for (int i = 0; i < 2; i++)
        {
            if (availableCards.Count == 0) break;
            
            int randomIndex = Random.Range(0, availableCards.Count);
            CardData randomCardData = availableCards[randomIndex];
            availableCards.RemoveAt(randomIndex);
            
            GameObject cardObject = Instantiate(cardPrefab, playerCardContainer);
            Card cardComponent = cardObject.GetComponent<Card>();
            
            if (cardComponent != null)
            {
                CardData clonedData = new CardData
                {
                    CardName = randomCardData.CardName,
                    Description = randomCardData.Description,
                    Effect = randomCardData.Effect,
                    CardImage = randomCardData.CardImage,
                    CardColor = playerCardColor
                };
                
                cardComponent.Initialize(this, clonedData);
                playerCards.Add(cardComponent);
                Debug.Log($"Created player card: {clonedData.CardName}");
            }
        }
        
        // Update UI for card selection
        if (uiManager != null)
        {
            uiManager.EnableCardSelection(true);
        }
        else
        {
            Debug.LogWarning("UIManager reference is missing in CardManager!");
        }
    }

    public void GenerateEnemyCards()
    {
        ClearEnemyCards();  // Очищаем старые карты противника
        
        if (cardPrefab == null || enemyCardContainer == null)
        {
            Debug.LogError("Required references are missing in CardManager!");
            return;
        }
        
        // Generate enemy cards
        List<CardData> availableCards = new List<CardData>(cardDataPool);
        for (int i = 0; i < 2; i++)
        {
            if (availableCards.Count == 0) break;
            
            int randomIndex = Random.Range(0, availableCards.Count);
            CardData randomCardData = availableCards[randomIndex];
            availableCards.RemoveAt(randomIndex);
            
            GameObject cardObject = Instantiate(cardPrefab, enemyCardContainer);
            Card cardComponent = cardObject.GetComponent<Card>();
            
            if (cardComponent != null)
            {
                CardData clonedData = new CardData
                {
                    CardName = randomCardData.CardName,
                    Description = randomCardData.Description,
                    Effect = randomCardData.Effect,
                    CardImage = randomCardData.CardImage,
                    CardColor = enemyCardColor
                };
                
                cardComponent.Initialize(this, clonedData);
                enemyCards.Add(cardComponent);
                Debug.Log($"Created enemy card: {clonedData.CardName}");
            }
        }
    }

    public void ClearPlayerCards()
    {
        Debug.Log("Clearing player cards");
        foreach (var card in playerCards)
        {
            if (card != null && card.gameObject != null)
            {
                // Запускаем анимацию исчезновения
                StartCoroutine(DestroyCardWithAnimation(card.gameObject));
            }
        }
        playerCards.Clear();
    }

    private IEnumerator DestroyCardWithAnimation(GameObject cardObject)
    {
        // Получаем компоненты для анимации
        CanvasGroup canvasGroup = cardObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = cardObject.AddComponent<CanvasGroup>();
        }

        // Плавно уменьшаем прозрачность
        float duration = 0.5f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            yield return null;
        }

        // Уничтожаем объект
        Destroy(cardObject);
    }

    private void ClearEnemyCards()
    {
        // Clear only enemy cards
        foreach (var card in enemyCards)
        {
            if (card != null)
            {
                Destroy(card.gameObject);
            }
        }
        enemyCards.Clear();
    }

    public void ClearAllCards()
    {
        ClearPlayerCards();
        ClearEnemyCards();
    }

    public void ResetState()
    {
        // Очищаем все списки карт
        playerCards.Clear();
        enemyCards.Clear();
        
        // Уничтожаем все оставшиеся игровые объекты карт
        foreach (Transform child in playerCardContainer)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in enemyCardContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Сбрасываем все флаги и состояния
        canUseCards = true;
    }

    public void OnCardSelected(Card card)
    {
        if (!canUseCards)
        {
            Debug.Log("Cannot use cards - already used this turn");
            return;
        }

        Debug.Log($"Card selected: {card.GetCardName()}");
        
        // Check if playerSquad is assigned
        if (playerSquad == null)
        {
            Debug.LogError("PlayerSquad reference is missing!");
            return;
        }

        // Check if squad has active character
        if (!playerSquad.HasActiveCharacter())
        {
            Debug.LogError("No active character in player squad!");
            return;
        }

        // Get the active character
        Character playerCharacter = playerSquad.GetActiveCharacter();
        if (playerCharacter == null)
        {
            Debug.LogError("Active character reference is null!");
            return;
        }

        Debug.Log($"Found active character: {playerCharacter.GetCharacterName()}");
        
        // Get card effect
        CardEffect effect = card.GetEffect();
        if (effect == null)
        {
            Debug.LogError("Card effect is null!");
            return;
        }

        Debug.Log($"Applying card effects to {playerCharacter.GetCharacterName()}:");
        Debug.Log($"Current character stats before effect:");
        Debug.Log($"- Health: {playerCharacter.GetCurrentHealth()}/{playerCharacter.GetMaxHealth()}");
        Debug.Log($"- Attack: {playerCharacter.GetAttackPower()}");
        Debug.Log($"- Armor: {playerCharacter.GetCurrentArmor()}/{playerCharacter.GetMaxArmor()}");
        Debug.Log($"- Energy: {playerCharacter.GetCurrentEnergy()}/{playerCharacter.GetMaxEnergy()}");
        
        bool effectApplied = false;

        // Apply health modification
        if (effect.HealthPercentageModifier != 0)
        {
            int currentHealth = playerCharacter.GetCurrentHealth();
            int maxHealth = playerCharacter.GetMaxHealth();
            int healthChange = Mathf.RoundToInt((maxHealth * effect.HealthPercentageModifier) / 100f);
            Debug.Log($"- Applying health modification: {healthChange}");
            playerCharacter.ModifyHealth(healthChange);
            effectApplied = true;
        }
        
        // Apply attack modification
        if (effect.AttackModifier != 0)
        {
            Debug.Log($"- Applying attack modification: {effect.AttackModifier}");
            playerCharacter.ModifyAttackPower(effect.AttackModifier);
            effectApplied = true;
        }
        
        // Apply armor modification
        if (effect.ArmorModifier != 0)
        {
            Debug.Log($"- Applying armor modification: {effect.ArmorModifier}");
            playerCharacter.ModifyArmor(effect.ArmorModifier);
            effectApplied = true;
        }
        
        // Apply energy modification
        if (effect.EnergyModifier != 0)
        {
            Debug.Log($"- Applying energy modification: {effect.EnergyModifier}");
            playerCharacter.ModifyEnergy(effect.EnergyModifier);
            effectApplied = true;
        }

        Debug.Log($"Current character stats after effect:");
        Debug.Log($"- Health: {playerCharacter.GetCurrentHealth()}/{playerCharacter.GetMaxHealth()}");
        Debug.Log($"- Attack: {playerCharacter.GetAttackPower()}");
        Debug.Log($"- Armor: {playerCharacter.GetCurrentArmor()}/{playerCharacter.GetMaxArmor()}");
        Debug.Log($"- Energy: {playerCharacter.GetCurrentEnergy()}/{playerCharacter.GetMaxEnergy()}");
        
        if (!effectApplied)
        {
            Debug.LogWarning("No effects were applied from the card!");
            return;
        }
        
        Debug.Log($"Successfully applied card effect: {card.GetCardName()} to {playerCharacter.GetCharacterName()}");
        
        // Disable cards for this turn and clear only player cards
        canUseCards = false;
        ClearPlayerCards();
    }

    public void UseEnemyCard()
    {
        if (enemyCards.Count == 0 || enemySquad == null || !enemySquad.HasActiveCharacter())
        {
            Debug.Log("Cannot use enemy card - no cards available or no active character");
            return;
        }

        // Выбираем случайную карту
        int randomIndex = Random.Range(0, enemyCards.Count);
        Card selectedCard = enemyCards[randomIndex];
        
        if (selectedCard == null)
        {
            Debug.LogError("Selected enemy card is null!");
            return;
        }

        Debug.Log($"Enemy using card: {selectedCard.GetCardName()}");
        
        // Получаем активного персонажа противника
        Character enemyCharacter = enemySquad.GetActiveCharacter();
        CardEffect effect = selectedCard.GetEffect();
        
        if (effect == null)
        {
            Debug.LogError("Card effect is null!");
            return;
        }

        // Применяем эффекты карты
        if (effect.HealthPercentageModifier != 0)
        {
            int healthChange = Mathf.RoundToInt((enemyCharacter.GetMaxHealth() * effect.HealthPercentageModifier) / 100f);
            enemyCharacter.ModifyHealth(healthChange);
        }
        
        if (effect.AttackModifier != 0)
        {
            enemyCharacter.ModifyAttackPower(effect.AttackModifier);
        }
        
        if (effect.ArmorModifier != 0)
        {
            enemyCharacter.ModifyArmor(effect.ArmorModifier);
        }
        
        if (effect.EnergyModifier != 0)
        {
            enemyCharacter.ModifyEnergy(effect.EnergyModifier);
        }

        // Показываем сообщение об использовании карты
        if (uiManager != null)
        {
            uiManager.ShowMessage($"Противник использует карту: {selectedCard.GetCardName()}");
        }

        // Очищаем карты противника с анимацией после использования
        ClearEnemyCardsWithAnimation();
    }

    public void ClearEnemyCardsWithAnimation()
    {
        Debug.Log("Clearing enemy cards with animation");
        foreach (var card in enemyCards)
        {
            if (card != null && card.gameObject != null)
            {
                StartCoroutine(DestroyCardWithAnimation(card.gameObject));
            }
        }
        enemyCards.Clear();
    }
} 