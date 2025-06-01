using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    [Header("Turn Information")]
    [SerializeField] private TextMeshProUGUI turnText;
    [SerializeField] private TextMeshProUGUI actionsRemainingText;
    [SerializeField] private GameObject playerTurnIndicator;
    [SerializeField] private GameObject enemyTurnIndicator;
    
    [Header("Message Display")]
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private float messageDisplayTime = 2f;
    [SerializeField] private GameObject messagePanel;
    
    [Header("Character Selection")]
    private List<GameObject> currentSelectionIndicators = new List<GameObject>();
    
    [Header("Action Buttons")]
    [SerializeField] private GameObject actionButtonsPanel;
    [SerializeField] private Button attackButton;
    [SerializeField] private Button superAttackButton;
    [SerializeField] private Button moveButton;
    [SerializeField] private Button endTurnButton;

    [Header("Attack Type Selection")]
    [SerializeField] private AttackTypeSelector attackTypeSelector;
    [SerializeField] private GameObject attackTypeSelectorPrefab;
    
    [Header("Card Selection")]
    [SerializeField] private GameObject cardSelectionPanel;
    
    [Header("Battle Results")]
    [SerializeField] private GameObject battleResultPanel;
    [SerializeField] private TextMeshProUGUI battleResultText;
    [SerializeField] private TextMeshProUGUI turnsCountText;
    [SerializeField] private TextMeshProUGUI damageDealtText;
    [SerializeField] private TextMeshProUGUI damageReceivedText;
    [SerializeField] private Button restartButton;
    
    [Header("Character Info")]
    [SerializeField] private GameObject playerInfoPanel;
    [SerializeField] private GameObject enemyInfoPanel;
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI enemyNameText;
    [SerializeField] private TextMeshProUGUI playerHealthText;
    [SerializeField] private TextMeshProUGUI enemyHealthText;
    [SerializeField] private TextMeshProUGUI playerEnergyText;
    [SerializeField] private TextMeshProUGUI enemyEnergyText;
    [SerializeField] private TextMeshProUGUI playerSuperAttackStatusText;
    [SerializeField] private TextMeshProUGUI enemySuperAttackStatusText;
    [SerializeField] private TextMeshProUGUI playerArmorText;
    [SerializeField] private TextMeshProUGUI enemyArmorText;
    [SerializeField] private TextMeshProUGUI playerAttackText;
    [SerializeField] private TextMeshProUGUI enemyAttackText;
    
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private BattleSystem battleSystem;
    [SerializeField] private CardManager cardManager;
    
    private Coroutine messageCoroutine;
    
    private Character playerCharacter;
    private Character enemyCharacter;
    
    public void Initialize()
    {
        Debug.Log("=== ИНИЦИАЛИЗАЦИЯ UI MANAGER ===");
        
        // Находим необходимые компоненты, если они не назначены
        if (gridManager == null)
            gridManager = FindAnyObjectByType<GridManager>();
        if (battleSystem == null)
            battleSystem = FindAnyObjectByType<BattleSystem>();
        if (cardManager == null)
            cardManager = FindAnyObjectByType<CardManager>();

        // Настраиваем кнопки действий
        if (attackButton != null)
            attackButton.onClick.AddListener(() => OnActionButtonClicked(ActionType.Attack));
        if (superAttackButton != null)
            superAttackButton.onClick.AddListener(() => OnActionButtonClicked(ActionType.SuperAttack));
        if (moveButton != null)
            moveButton.onClick.AddListener(() => OnActionButtonClicked(ActionType.Move));
        if (endTurnButton != null)
            endTurnButton.onClick.AddListener(OnEndTurnButtonClicked);
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);

        // Создаем панель выбора типа атаки, если она не существует
        if (attackTypeSelector == null && attackTypeSelectorPrefab != null)
        {
            GameObject selectorObj = Instantiate(attackTypeSelectorPrefab, actionButtonsPanel.transform.parent);
            attackTypeSelector = selectorObj.GetComponent<AttackTypeSelector>();
            if (attackTypeSelector != null)
            {
                attackTypeSelector.GetComponent<AttackTypeSelector>().enabled = true;
                var selector = attackTypeSelector.GetComponent<AttackTypeSelector>();
                selector.Initialize(battleSystem, this);
            }
        }

        // Показываем нужные панели
        ShowActionButtons(true);
        if (cardSelectionPanel != null)
            cardSelectionPanel.SetActive(true);
            
        // Обновляем информацию о персонажах
        UpdateCharacterInfo();
        
        // Подписываемся на события
        SubscribeToCharacterEvents();
        
        Debug.Log("=== ЗАВЕРШЕНИЕ ИНИЦИАЛИЗАЦИИ UI MANAGER ===");
    }

    private void Start()
    {
        Initialize();
    }
    
    public void HideAllPanels()
    {
        if (messagePanel != null)
            messagePanel.SetActive(false);
            
        if (actionButtonsPanel != null)
            actionButtonsPanel.SetActive(false);
            
        if (cardSelectionPanel != null)
            cardSelectionPanel.SetActive(false);
            
        if (battleResultPanel != null)
            battleResultPanel.SetActive(false);
            
        if (playerTurnIndicator != null)
            playerTurnIndicator.SetActive(false);
            
        if (enemyTurnIndicator != null)
            enemyTurnIndicator.SetActive(false);
    }
    
    public void UpdateTurnCounter(int currentTurn, int maxTurns)
    {
        if (turnText != null)
        {
            turnText.text = $"Ход {currentTurn}/{maxTurns}";
        }
    }
    
    public void UpdateActionsRemaining(int actionsRemaining)
    {
        if (actionsRemainingText != null)
        {
            actionsRemainingText.text = $"Осталось действий: {actionsRemaining}";
        }
        
        // Обновляем состояние кнопок действий
        UpdateActionButtonsState();
    }
    
    private void UpdateActionButtonsState()
    {
        UpdatePlayerSuperAttackState();
        UpdateEnemySuperAttackState();
    }
    
    private void UpdatePlayerSuperAttackState()
    {
        Squad playerSquad = FindAnyObjectByType<Squad>();
        if (playerSquad != null && playerSquad.HasActiveCharacter())
        {
            Character activeCharacter = playerSquad.GetActiveCharacter();
            bool canUseSuperAttack = activeCharacter.CanPerformSuperAttack();
            
            if (superAttackButton != null)
            {
                superAttackButton.interactable = canUseSuperAttack && actionButtonsPanel.activeSelf;
            }
            
            // Обновляем текст статуса супер атаки игрока
            if (playerSuperAttackStatusText != null)
            {
                playerSuperAttackStatusText.text = canUseSuperAttack ? "Активен" : "Не активен";
                playerSuperAttackStatusText.color = canUseSuperAttack ? Color.green : Color.red;
            }
            
            Debug.Log($"Обновление состояния кнопки суператаки игрока. Доступность: {superAttackButton?.interactable}, Энергия: {activeCharacter.GetCurrentEnergy()}/{activeCharacter.GetMaxEnergy()}, Может использовать: {canUseSuperAttack}");
        }
        else
        {
            if (superAttackButton != null)
            {
                superAttackButton.interactable = false;
            }
            if (playerSuperAttackStatusText != null)
            {
                playerSuperAttackStatusText.text = "Не активен";
                playerSuperAttackStatusText.color = Color.red;
            }
        }
    }
    
    private void UpdateEnemySuperAttackState()
    {
        Squad[] squads = FindObjectsByType<Squad>(FindObjectsSortMode.None);
        if (squads.Length >= 2)
        {
            Squad enemySquad = squads[1]; // Предполагаем, что вражеский отряд второй
            if (enemySquad != null && enemySquad.HasActiveCharacter())
            {
                Character activeCharacter = enemySquad.GetActiveCharacter();
                bool canUseSuperAttack = activeCharacter.CanPerformSuperAttack();
                
                // Обновляем текст статуса супер атаки противника
                if (enemySuperAttackStatusText != null)
                {
                    enemySuperAttackStatusText.text = canUseSuperAttack ? "Активен" : "Не активен";
                    enemySuperAttackStatusText.color = canUseSuperAttack ? Color.green : Color.red;
                }
                
                Debug.Log($"Обновление состояния суператаки противника. Энергия: {activeCharacter.GetCurrentEnergy()}/{activeCharacter.GetMaxEnergy()}, Может использовать: {canUseSuperAttack}");
            }
            else if (enemySuperAttackStatusText != null)
            {
                enemySuperAttackStatusText.text = "Не активен";
                enemySuperAttackStatusText.color = Color.red;
            }
        }
    }
    
    public void SetTurnIndicator(bool isPlayerTurn)
    {
        if (playerTurnIndicator != null)
            playerTurnIndicator.SetActive(isPlayerTurn);
            
        if (enemyTurnIndicator != null)
            enemyTurnIndicator.SetActive(!isPlayerTurn);
    }
    
    public void ShowMessage(string message)
    {
        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
        }
        messageCoroutine = StartCoroutine(ShowMessageCoroutine(message));
    }
    
    private IEnumerator ShowMessageCoroutine(string message)
    {
        if (messagePanel != null && messageText != null)
        {
            messagePanel.SetActive(true);
            messageText.text = message;
            yield return new WaitForSeconds(messageDisplayTime);
            messagePanel.SetActive(false);
        }
    }
    
    public void EnablePlayerSelection(bool enable)
    {
        Squad playerSquad = FindAnyObjectByType<Squad>();
        if (playerSquad == null)
            return;
            
        ClearSelectionIndicators();
        
        if (enable)
        {
            foreach (Character character in playerSquad.GetAliveMembers())
            {
                character.GetComponent<CharacterSelector>()?.Initialize(character, this);
            }
        }
    }
    
    public void EnableEnemySelection(bool enable)
    {
        Squad[] squads = FindObjectsByType<Squad>(FindObjectsSortMode.None);
        if (squads.Length < 2)
            return;
            
        Squad enemySquad = squads[1]; // Assuming enemy squad is the second one
        if (enemySquad == null)
            return;
            
        ClearSelectionIndicators();
        
        if (enable)
        {
            foreach (Character character in enemySquad.GetAliveMembers())
            {
                character.GetComponent<CharacterSelector>()?.Initialize(character, this);
            }
        }
    }
    
    private void ClearSelectionIndicators()
    {
        foreach (GameObject indicator in currentSelectionIndicators)
        {
            if (indicator != null)
            {
                Destroy(indicator);
            }
        }
        
        currentSelectionIndicators.Clear();
    }
    
    public void OnCharacterSelected(Character character)
    {
        Squad[] squads = FindObjectsByType<Squad>(FindObjectsSortMode.None);
        if (squads.Length < 2)
            return;

        Squad playerSquad = squads[0];
        Squad enemySquad = squads[1];
        
        if (playerSquad.GetAliveMembers().Contains(character))
        {
            playerSquad.SetActiveCharacter(character);
            ShowActionButtons(true);
            ShowMessage($"Выбран персонаж: {character.GetCharacterName()}");
        }
        else if (enemySquad.GetAliveMembers().Contains(character))
        {
            if (battleSystem != null)
            {
                battleSystem.SelectTarget(character);
                ClearSelectionIndicators();
            }
        }
    }
    
    private void HighlightCharacter(Character character)
    {
        ClearSelectionIndicators();
        // Visual feedback can be implemented directly on the character if needed
    }
    
    public void HighlightEnemy(Character character)
    {
        ClearSelectionIndicators();
        // Visual feedback can be implemented directly on the character if needed
    }
    
    public void ShowActionButtons(bool show)
    {
        if (actionButtonsPanel != null)
        {
            actionButtonsPanel.SetActive(show);
        }
        
        // Скрываем селектор типа атаки при скрытии кнопок действий
        if (!show && attackTypeSelector != null)
        {
            attackTypeSelector.HideSelector();
        }
        
        // Обновляем состояние кнопок
        UpdateActionButtonsState();
        
        // Обновляем состояние кнопки завершения хода
        if (endTurnButton != null)
        {
            endTurnButton.gameObject.SetActive(show);
        }
    }
    
    public void OnActionButtonClicked(ActionType actionType)
    {
        if (battleSystem != null)
        {
            if (actionType == ActionType.Attack)
            {
                // Показываем селектор типа атаки вместо сразу активации выбора цели
                if (attackTypeSelector != null)
                {
                    Debug.Log("Показываем панель выбора типа атаки");
                    attackTypeSelector.ShowSelector();
                }
                else
                {
                    Debug.LogError("AttackTypeSelector не найден!");
                }
            }
            else
            {
                battleSystem.SelectAction(actionType);
            }
        }
    }
    
    public void OnGridCellClicked(Vector2Int position)
    {
        if (battleSystem != null && battleSystem.GetPlayerSquad().HasActiveCharacter())
        {
            Character activeCharacter = battleSystem.GetPlayerSquad().GetActiveCharacter();
            Vector2Int currentPos = activeCharacter.GetPosition();

            // Проверяем, что движение происходит только по прямой линии
            bool isHorizontalMove = currentPos.y == position.y && currentPos.x != position.x;
            bool isVerticalMove = currentPos.x == position.x && currentPos.y != position.y;

            if (isHorizontalMove || isVerticalMove)
            {
                gridManager.PlaceCharacter(activeCharacter, position);
                battleSystem.OnActionCompleted();
            }
            else
            {
                // Если движение не по прямой, показываем сообщение
                ShowMessage("Можно двигаться только по прямой линии!");
            }
        }
    }
    
    public void EnableCardSelection(bool enable)
    {
        if (cardSelectionPanel != null)
        {
            cardSelectionPanel.SetActive(enable);
            
            // Show only player cards when it's player's turn
            if (battleSystem != null)
            {
                Transform playerCardContainer = cardManager?.transform.Find("PlayerCardContainer");
                Transform enemyCardContainer = cardManager?.transform.Find("EnemyCardContainer");
                
                if (playerCardContainer != null)
                {
                    playerCardContainer.gameObject.SetActive(enable);
                }
                
                if (enemyCardContainer != null)
                {
                    enemyCardContainer.gameObject.SetActive(false); // Always hide enemy cards during selection
                }
            }
        }
    }
    
    public void ShowBattleResult(string message, int turns, int damageDealt, int damageReceived)
    {
        if (battleResultPanel != null)
        {
            battleResultPanel.SetActive(true);
            
            if (battleResultText != null)
                battleResultText.text = message;
                
            if (turnsCountText != null)
                turnsCountText.text = $"Количество ходов: {turns}";
                
            if (damageDealtText != null)
                damageDealtText.text = $"Нанесено урона: {damageDealt}";
                
            if (damageReceivedText != null)
                damageReceivedText.text = $"Получено урона: {damageReceived}";
        }
    }
    
    private void RestartGame()
    {
        // Вызываем метод перезапуска из GameManager
        GameManager.Instance.RestartGame();
    }

    public void UnsubscribeFromAllEvents()
    {
        // Отписываемся от всех событий персонажей
        UnsubscribeFromCharacterEvents();

        // Отписываемся от событий кнопок
        if (attackButton != null)
            attackButton.onClick.RemoveAllListeners();
            
        if (superAttackButton != null)
            superAttackButton.onClick.RemoveAllListeners();
            
        if (moveButton != null)
            moveButton.onClick.RemoveAllListeners();
            
        if (endTurnButton != null)
            endTurnButton.onClick.RemoveAllListeners();
            
        if (restartButton != null)
            restartButton.onClick.RemoveAllListeners();

        // Очищаем все корутины
        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
            messageCoroutine = null;
        }
    }

    private void UpdateCharacterInfo()
    {
        Squad[] squads = FindObjectsByType<Squad>(FindObjectsSortMode.None);
        if (squads.Length >= 2)
        {
            Squad playerSquad = squads[0];
            Squad enemySquad = squads[1];

            if (playerSquad != null && playerSquad.HasActiveCharacter())
            {
                playerCharacter = playerSquad.GetActiveCharacter();
                if (playerNameText != null)
                    playerNameText.text = playerCharacter.GetCharacterName();
                UpdatePlayerHealth(playerCharacter.GetCurrentHealth(), playerCharacter.GetMaxHealth());
                UpdatePlayerEnergy(playerCharacter.GetCurrentEnergy(), playerCharacter.GetMaxEnergy());
                UpdatePlayerArmor(playerCharacter.GetCurrentArmor());
                UpdatePlayerAttack(playerCharacter.GetAttackPower());
            }

            if (enemySquad != null && enemySquad.HasActiveCharacter())
            {
                enemyCharacter = enemySquad.GetActiveCharacter();
                if (enemyNameText != null)
                    enemyNameText.text = enemyCharacter.GetCharacterName();
                UpdateEnemyHealth(enemyCharacter.GetCurrentHealth(), enemyCharacter.GetMaxHealth());
                UpdateEnemyEnergy(enemyCharacter.GetCurrentEnergy(), enemyCharacter.GetMaxEnergy());
                UpdateEnemyArmor(enemyCharacter.GetCurrentArmor());
                UpdateEnemyAttack(enemyCharacter.GetAttackPower());
            }
        }
    }

    private void SubscribeToCharacterEvents()
    {
        if (playerCharacter != null)
        {
            playerCharacter.OnHealthChanged += UpdatePlayerHealth;
            playerCharacter.OnEnergyChanged += UpdatePlayerEnergy;
            playerCharacter.OnArmorChanged += UpdatePlayerArmor;
            playerCharacter.OnStatsChanged += () => UpdatePlayerAttack(playerCharacter.GetAttackPower());
        }

        if (enemyCharacter != null)
        {
            enemyCharacter.OnHealthChanged += UpdateEnemyHealth;
            enemyCharacter.OnEnergyChanged += UpdateEnemyEnergy;
            enemyCharacter.OnArmorChanged += UpdateEnemyArmor;
            enemyCharacter.OnStatsChanged += () => UpdateEnemyAttack(enemyCharacter.GetAttackPower());
        }
    }

    private void UnsubscribeFromCharacterEvents()
    {
        if (playerCharacter != null)
        {
            playerCharacter.OnHealthChanged -= UpdatePlayerHealth;
            playerCharacter.OnEnergyChanged -= UpdatePlayerEnergy;
            playerCharacter.OnArmorChanged -= UpdatePlayerArmor;
            playerCharacter.OnStatsChanged -= () => UpdatePlayerAttack(playerCharacter.GetAttackPower());
        }

        if (enemyCharacter != null)
        {
            enemyCharacter.OnHealthChanged -= UpdateEnemyHealth;
            enemyCharacter.OnEnergyChanged -= UpdateEnemyEnergy;
            enemyCharacter.OnArmorChanged -= UpdateEnemyArmor;
            enemyCharacter.OnStatsChanged -= () => UpdateEnemyAttack(enemyCharacter.GetAttackPower());
        }
    }

    private void UpdatePlayerHealth(int currentHealth, int maxHealth)
    {
        if (playerHealthText != null)
        {
            playerHealthText.text = $"Здоровье {currentHealth}/{maxHealth}";
        }
    }

    private void UpdateEnemyHealth(int currentHealth, int maxHealth)
    {
        if (enemyHealthText != null)
        {
            enemyHealthText.text = $"Здоровье {currentHealth}/{maxHealth}";
        }
    }

    private void UpdatePlayerEnergy(float currentEnergy, float maxEnergy)
    {
        if (playerEnergyText != null)
        {
            playerEnergyText.text = $"Энергия {Mathf.Floor(currentEnergy)}/{maxEnergy}";
        }
        UpdatePlayerSuperAttackState();
    }

    private void UpdateEnemyEnergy(float currentEnergy, float maxEnergy)
    {
        if (enemyEnergyText != null)
        {
            enemyEnergyText.text = $"Энергия {Mathf.Floor(currentEnergy)}/{maxEnergy}";
        }
        UpdateEnemySuperAttackState();
    }

    private void UpdatePlayerArmor(int currentArmor)
    {
        if (playerArmorText != null)
        {
            playerArmorText.text = $"Броня {currentArmor}";
        }
    }

    private void UpdateEnemyArmor(int currentArmor)
    {
        if (enemyArmorText != null)
        {
            enemyArmorText.text = $"Броня {currentArmor}";
        }
    }

    private void UpdatePlayerAttack(int attackPower)
    {
        if (playerAttackText != null)
        {
            playerAttackText.text = $"Атака {attackPower}";
        }
    }

    private void UpdateEnemyAttack(int attackPower)
    {
        if (enemyAttackText != null)
        {
            enemyAttackText.text = $"Атака {attackPower}";
        }
    }

    private void OnEndTurnButtonClicked()
    {
        if (battleSystem != null)
        {
            battleSystem.EndTurnButtonClicked();
        }
    }
}

// Helper class for character selection
public class CharacterSelector : MonoBehaviour
{
    private Character character;
    private UIManager uiManager;
    
    public void Initialize(Character character, UIManager manager)
    {
        this.character = character;
        this.uiManager = manager;
    }
    
    private void OnMouseDown()
    {
        if (character != null && uiManager != null)
        {
            uiManager.OnCharacterSelected(character);
        }
    }
} 