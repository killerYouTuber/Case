using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class BattleSystem : MonoBehaviour
{
    [Header("Squad References")]
    [SerializeField] private Squad playerSquad;
    [SerializeField] private Squad enemySquad;
    
    [Header("Battle Settings")]
    [SerializeField] private float actionDelay = 0.5f;
    [SerializeField] private int maxTurns = 10; // Максимальное количество ходов
    private int currentTurn = 1; // Текущий ход
    
    [Header("UI References")]
    [SerializeField] private UIManager uiManager;
    [SerializeField] private CardManager cardManager;
    [SerializeField] private AttackTypeSelector weaponSelector;

    [Header("Grid Settings")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private int attackRange = 2;
    [SerializeField] private int superAttackRange = 3;

    [Header("Attack Button")]
    [SerializeField] private Button attackButton;

    private bool isPlayerTurn = true;
    private int actionsRemaining = 3;
    private ActionType pendingAction = ActionType.None;
    private bool weaponSelected = false;

    private BattleStatistics battleStats;

    private void Awake()
    {
        battleStats = new BattleStatistics();
        
        if (playerSquad == null || enemySquad == null)
        {
            Debug.LogError("Squads not assigned in BattleSystem!");
            return;
        }

        // Find required components if not assigned
        if (gridManager == null)
        {
            gridManager = FindAnyObjectByType<GridManager>();
            if (gridManager == null)
            {
                Debug.LogError("GridManager not found in the scene!");
                return;
            }
        }

        if (uiManager == null)
        {
            uiManager = FindAnyObjectByType<UIManager>();
            if (uiManager == null)
            {
                Debug.LogError("UIManager not found in the scene!");
                return;
            }
        }

        // Убедимся, что отряды правильно инициализированы
        if (!playerSquad.HasActiveCharacter())
        {
            Character firstAlivePlayer = playerSquad.GetAliveMembers().FirstOrDefault();
            if (firstAlivePlayer != null)
            {
                playerSquad.SetActiveCharacter(firstAlivePlayer);
            }
            else
            {
                Debug.LogError("No alive characters in player squad!");
            }
        }

        if (!enemySquad.HasActiveCharacter())
        {
            Character firstAliveEnemy = enemySquad.GetAliveMembers().FirstOrDefault();
            if (firstAliveEnemy != null)
            {
                enemySquad.SetActiveCharacter(firstAliveEnemy);
            }
            else
            {
                Debug.LogError("No alive characters in enemy squad!");
            }
        }

        // Subscribe to damage events for player squad
        foreach (Character character in playerSquad.GetAliveMembers())
        {
            if (character != null)
            {
                character.OnDamageDealt += OnPlayerDamageDealt;
                character.OnDamageTaken += OnPlayerDamageTaken;
            }
        }
    }

    private void Start()
    {
        Debug.Log("=== НАЧАЛО ИНИЦИАЛИЗАЦИИ БОЕВОЙ СИСТЕМЫ ===");

        if (gridManager == null)
        {
            Debug.LogError("GridManager reference is missing!");
            return;
        }

        if (playerSquad == null || enemySquad == null)
        {
            Debug.LogError("One or both squads are missing!");
            return;
        }

        if (attackButton != null)
        {
            attackButton.onClick.AddListener(() => SelectAction(ActionType.Attack));
        }

        // Проверяем состояние отрядов
        Debug.Log($"Player squad alive members: {playerSquad.GetAliveMembersCount()}");
        Debug.Log($"Enemy squad alive members: {enemySquad.GetAliveMembersCount()}");

        // Размещаем персонажей в случайных клетках
        if (playerSquad.HasActiveCharacter() && enemySquad.HasActiveCharacter())
        {
            Character playerCharacter = playerSquad.GetActiveCharacter();
            Character enemyCharacter = enemySquad.GetActiveCharacter();

            Debug.Log($"Active player character: {playerCharacter.GetCharacterName()}, Active: {playerCharacter.gameObject.activeSelf}");
            Debug.Log($"Active enemy character: {enemyCharacter.GetCharacterName()}, Active: {enemyCharacter.gameObject.activeSelf}");

            // Убеждаемся, что персонажи активны
            if (!playerCharacter.gameObject.activeSelf)
            {
                Debug.LogWarning("Player character was inactive, activating");
                playerCharacter.gameObject.SetActive(true);
            }
            if (!enemyCharacter.gameObject.activeSelf)
            {
                Debug.LogWarning("Enemy character was inactive, activating");
                enemyCharacter.gameObject.SetActive(true);
            }

            Vector2Int playerPos = GetRandomEmptyPosition();
            Vector2Int enemyPos;
            do
            {
                enemyPos = GetRandomEmptyPosition();
            } while (enemyPos == playerPos);

            Debug.Log($"Placing player at position: {playerPos}");
            gridManager.PlaceCharacter(playerCharacter, playerPos);

            Debug.Log($"Placing enemy at position: {enemyPos}");
            gridManager.PlaceCharacter(enemyCharacter, enemyPos);

            // Проверяем, что персонажи успешно размещены
            Debug.Log($"Player character position after placement: {playerCharacter.transform.position}");
            Debug.Log($"Enemy character position after placement: {enemyCharacter.transform.position}");
        }
        else
        {
            Debug.LogError("One or both squads don't have active characters!");
            return;
        }
        
        // Инициализируем счетчик ходов
        if (uiManager != null)
        {
            uiManager.UpdateTurnCounter(currentTurn, maxTurns);
            uiManager.UpdateActionsRemaining(actionsRemaining);
        }
        else
        {
            Debug.LogError("UIManager reference is missing!");
        }

        Debug.Log("=== ЗАВЕРШЕНИЕ ИНИЦИАЛИЗАЦИИ БОЕВОЙ СИСТЕМЫ ===");
    }

    private Vector2Int GetRandomEmptyPosition()
    {
        List<Vector2Int> emptyPositions = new List<Vector2Int>();
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (IsPositionEmpty(pos))
                {
                    emptyPositions.Add(pos);
                }
            }
        }
        return emptyPositions[Random.Range(0, emptyPositions.Count)];
    }

    private bool IsPositionEmpty(Vector2Int pos)
    {
        if (!gridManager.IsValidPosition(pos)) return false;
        
        // Проверяем, нет ли на клетке других персонажей
        Vector3 worldPos = gridManager.GetWorldPosition(pos);
        Collider2D[] colliders = Physics2D.OverlapPointAll(worldPos);
        foreach (var collider in colliders)
        {
            if (collider.GetComponent<Character>() != null)
            {
                return false;
            }
        }
        return true;
    }

    public void StartNewTurn()
    {
        actionsRemaining = 3;
        isPlayerTurn = true;
        
        // Увеличиваем счетчик ходов
        currentTurn++;
        battleStats.IncrementTurn();
        if (uiManager != null)
        {
            uiManager.UpdateTurnCounter(currentTurn, maxTurns);
        }
        
        // Генерируем карты для обоих сторон в начале хода игрока
        if (cardManager != null)
        {
            cardManager.StartNewTurn();
        }
        
        uiManager.UpdateActionsRemaining(actionsRemaining);
        uiManager.SetTurnIndicator(isPlayerTurn);
        
        if (isPlayerTurn)
        {
            uiManager.EnablePlayerSelection(true);
            uiManager.ShowActionButtons(true);
        }
    }

    public void SelectAction(ActionType actionType)
    {
        if (!isPlayerTurn || actionsRemaining <= 0)
            return;
            
        pendingAction = actionType;
        Character activeCharacter = playerSquad.GetActiveCharacter();
        
        switch (actionType)
        {
            case ActionType.Move:
                uiManager.ShowMessage("Выберите позицию для перемещения");
                gridManager.StartMoveMode(activeCharacter);
                break;
                
            case ActionType.Attack:
                if (!weaponSelected)
                {
                    uiManager.ShowMessage("Выберите тип оружия");
                    if (weaponSelector != null)
                    {
                        weaponSelector.ShowSelector();
                    }
                }
                else
                {
                    uiManager.ShowMessage("Выберите цель для атаки");
                    gridManager.StartAttackMode(activeCharacter);
                    weaponSelected = false;
                }
                break;
                
            case ActionType.SuperAttack:
                if (activeCharacter.CanPerformSuperAttack())
                {
                    uiManager.ShowMessage("Выберите цель для суператаки");
                    if (activeCharacter is PlayerCharacter superAttackChar)
                    {
                        superAttackChar.SetAttackRange(superAttackRange);
                    }
                    gridManager.StartAttackMode(activeCharacter);
                }
                else
                {
                    uiManager.ShowMessage("Недостаточно энергии для суператаки!");
                }
                break;
        }
    }

    public void SelectTarget(Character target)
    {
        if (!isPlayerTurn || actionsRemaining <= 0 || pendingAction == ActionType.None)
            return;

        Character activeCharacter = playerSquad.GetActiveCharacter();
        if (activeCharacter == null || target == null)
            return;

        // Проверяем дистанцию до цели
        Vector2Int attackerPos = activeCharacter.GetPosition();
        Vector2Int targetPos = target.GetPosition();
        int distance = Mathf.Abs(targetPos.x - attackerPos.x) + Mathf.Abs(targetPos.y - attackerPos.y);

        bool isValidTarget = false;
        switch (pendingAction)
        {
            case ActionType.Attack:
                isValidTarget = distance <= attackRange;
                break;
            case ActionType.SuperAttack:
                isValidTarget = distance <= superAttackRange && activeCharacter.CanPerformSuperAttack();
                break;
        }

        if (!isValidTarget)
        {
            uiManager.ShowMessage("Цель слишком далеко");
            return;
        }

        // Выполняем атаку
        switch (pendingAction)
        {
            case ActionType.Attack:
                activeCharacter.PerformAttack(target);
                break;
            case ActionType.SuperAttack:
                activeCharacter.PerformSuperAttack(target);
                if (activeCharacter is PlayerCharacter playerChar)
                {
                    playerChar.RestoreDefaultAttackRange();
                }
                // Обновляем состояние кнопки суператаки после её использования
                uiManager.ShowActionButtons(true);
                break;
        }

        actionsRemaining--;
        uiManager.UpdateActionsRemaining(actionsRemaining);
        uiManager.EnableEnemySelection(false);
        pendingAction = ActionType.None;

        if (actionsRemaining <= 0)
        {
            EndPlayerTurn();
        }
    }

    public void OnCellSelected(Vector2Int gridPosition)
    {
        if (!isPlayerTurn || actionsRemaining <= 0 || pendingAction != ActionType.Move)
            return;

        Character activeCharacter = playerSquad.GetActiveCharacter();
        if (activeCharacter != null)
        {
            gridManager.PlaceCharacter(activeCharacter, gridPosition);
            actionsRemaining--;
            uiManager.UpdateActionsRemaining(actionsRemaining);
            
            if (actionsRemaining <= 0)
            {
                EndPlayerTurn();
            }
        }
        
        gridManager.CancelMoveMode();
    }

    private void EndPlayerTurn()
    {
        isPlayerTurn = false;
        uiManager.ShowActionButtons(false);
        uiManager.SetTurnIndicator(false);
        
        // Очищаем неиспользованные карты игрока
        if (cardManager != null)
        {
            cardManager.ClearPlayerCards();
        }
        
        // Показываем сообщение о переходе хода
        uiManager.ShowMessage("Ход переходит к противнику...");
        
        // Запускаем ход противника с задержкой
        StartCoroutine(StartEnemyTurnWithDelay());
    }

    private IEnumerator StartEnemyTurnWithDelay()
    {
        // Ждем 2 секунды перед началом хода противника
        yield return new WaitForSeconds(2f);
        
        // Запускаем ход противника
        StartCoroutine(ExecuteEnemyTurn());
    }

    private IEnumerator ExecuteEnemyTurn()
    {
        Debug.Log("=== НАЧАЛО ХОДА ПРОТИВНИКА ===");
        
        if (enemySquad == null || !enemySquad.HasActiveCharacter())
        {
            Debug.LogWarning("No active enemy character found!");
            StartNewTurn();
            yield break;
        }

        Character enemyCharacter = enemySquad.GetActiveCharacter();
        
        if (playerSquad == null || !playerSquad.HasActiveCharacter())
        {
            Debug.LogWarning("No active player character found!");
            StartNewTurn();
            yield break;
        }

        Character playerCharacter = playerSquad.GetActiveCharacter();
        
        // Проверяем, живы ли оба персонажа
        if (!enemyCharacter.IsAlive() || !playerCharacter.IsAlive())
        {
            Debug.Log("One of the characters is dead, ending turn");
            CheckAndHandleGameOver();
            yield break;
        }

        // Используем карту противника, если есть
        if (cardManager != null)
        {
            cardManager.UseEnemyCard();
            yield return new WaitForSeconds(actionDelay);
        }
        
        actionsRemaining = 3;
        uiManager.UpdateActionsRemaining(actionsRemaining);
        
        while (actionsRemaining > 0)
        {
            // Обновляем позиции
            Vector2Int enemyPos = enemyCharacter.GetPosition();
            Vector2Int playerPos = playerCharacter.GetPosition();
            
            // Проверяем дистанцию до игрока
            int distanceToPlayer = Mathf.Abs(enemyPos.x - playerPos.x) + Mathf.Abs(enemyPos.y - playerPos.y);
            
            // Проверяем возможность супер-атаки
            if (distanceToPlayer <= superAttackRange && enemyCharacter.CanPerformSuperAttack() && playerCharacter.IsAlive())
            {
                Debug.Log("Противник использует супер-атаку");
                enemyCharacter.PerformSuperAttack(playerCharacter);
                uiManager.ShowMessage($"{enemyCharacter.GetCharacterName()} использует супер атаку!");
                yield return new WaitForSeconds(actionDelay);
                actionsRemaining--;
                
                // Проверяем состояние после атаки
                if (!playerCharacter.IsAlive())
                {
                    CheckAndHandleGameOver();
                    yield break;
                }
            }
            else if (distanceToPlayer <= attackRange && playerCharacter.IsAlive())
            {
                // Выбираем тип атаки на основе дистанции
                AttackType selectedAttackType;
                
                if (distanceToPlayer <= 1)
                {
                    // На близкой дистанции используем меч
                    selectedAttackType = AttackType.Sword;
                }
                else if (distanceToPlayer <= 2)
                {
                    // На средней дистанции используем магию
                    selectedAttackType = AttackType.Magic;
                }
                else
                {
                    // На дальней дистанции используем лук
                    selectedAttackType = AttackType.Bow;
                }
                
                // Устанавливаем выбранный тип атаки
                enemyCharacter.SetAttackType(selectedAttackType);
                
                Debug.Log($"Противник выполняет атаку типа {selectedAttackType}");
                enemyCharacter.PerformAttack(playerCharacter);
                uiManager.ShowMessage($"{enemyCharacter.GetCharacterName()} атакует используя {GetAttackTypeName(selectedAttackType)}!");
                yield return new WaitForSeconds(actionDelay);
                actionsRemaining--;
                
                // Проверяем состояние после атаки
                if (!playerCharacter.IsAlive())
                {
                    CheckAndHandleGameOver();
                    yield break;
                }
            }
            else
            {
                Debug.Log("Противник пытается приблизиться к игроку");
                // Если далеко - двигаемся к игроку
                Vector2Int targetPos = playerPos;
                Vector2Int currentPos = enemyPos;
                
                // Определяем, по какой оси двигаться
                int dx = targetPos.x - currentPos.x;
                int dy = targetPos.y - currentPos.y;
                
                Vector2Int moveDirection;
                Vector2Int nextPos;
                
                // Выбираем движение по той оси, где расстояние больше
                if (Mathf.Abs(dx) >= Mathf.Abs(dy))
                {
                    // Движение по горизонтали
                    moveDirection = new Vector2Int((int)Mathf.Sign(dx), 0);
                    // Вычисляем максимально возможное перемещение (не более 3 клеток)
                    int moveDistance = Mathf.Min(3, Mathf.Abs(dx));
                    nextPos = currentPos + new Vector2Int(moveDistance * (int)Mathf.Sign(dx), 0);
                }
                else
                {
                    // Движение по вертикали
                    moveDirection = new Vector2Int(0, (int)Mathf.Sign(dy));
                    // Вычисляем максимально возможное перемещение (не более 3 клеток)
                    int moveDistance = Mathf.Min(3, Mathf.Abs(dy));
                    nextPos = currentPos + new Vector2Int(0, moveDistance * (int)Mathf.Sign(dy));
                }

                // Пытаемся выполнить перемещение
                bool moved = enemyCharacter.TryMoveTo(nextPos);
                
                if (moved)
                {
                    while (enemyCharacter.IsMoving())
                    {
                        yield return null;
                    }
                    
                    // Вычисляем, на сколько клеток фактически переместились
                    Vector2Int finalPos = enemyCharacter.GetPosition();
                    int movesMade = Mathf.Abs(finalPos.x - currentPos.x) + Mathf.Abs(finalPos.y - currentPos.y);
                    
                    Debug.Log($"Противник переместился на {movesMade} клеток");
                    uiManager.ShowMessage($"{enemyCharacter.GetCharacterName()} переместился на {movesMade} {GetCellsWord(movesMade)}");
                    yield return new WaitForSeconds(actionDelay);
                }
                else
                {
                    Debug.Log("Противник не смог переместиться");
                    uiManager.ShowMessage($"{enemyCharacter.GetCharacterName()} не смог переместиться");
                    yield return new WaitForSeconds(actionDelay);
                }
                
                actionsRemaining--;
            }
            
            Debug.Log($"Осталось действий: {actionsRemaining}");
            uiManager.UpdateActionsRemaining(actionsRemaining);
            yield return new WaitForSeconds(actionDelay);
        }
        
        Debug.Log("=== КОНЕЦ ХОДА ПРОТИВНИКА ===");
        StartNewTurn();
    }

    // Вспомогательный метод для правильного склонения слова "клетка"
    private string GetCellsWord(int number)
    {
        if (number % 10 == 1 && number % 100 != 11)
            return "клетку";
        if ((number % 10 == 2 || number % 10 == 3 || number % 10 == 4) && 
            (number % 100 < 10 || number % 100 > 20))
            return "клетки";
        return "клеток";
    }
    
    public void OnCharacterMoved(Character character)
    {
        // Этот метод вызывается после перемещения персонажа
        // Здесь можно добавить дополнительную логику, например,
        // проверку условий победы или специальных эффектов
    }
    
    public Squad GetPlayerSquad()
    {
        return playerSquad;
    }

    public Squad GetEnemySquad()
    {
        return enemySquad;
    }
    
    public void CheckAndHandleGameOver()
    {
        if (CheckBattleEnd())
        {
            // Определяем результат битвы
            BattleResult result = DetermineBattleResult();
            string resultMessage = "";
            
            switch (result)
            {
                case BattleResult.PlayerWin:
                    resultMessage = "Победа! Враг повержен!";
                    break;
                case BattleResult.EnemyWin:
                    resultMessage = "Поражение! Ваш герой пал в бою!";
                    break;
                case BattleResult.Draw:
                    resultMessage = "Ничья! Все герои пали в бою!";
                    break;
            }
            
            // Показываем экран с результатом и статистикой
            if (uiManager != null)
            {
                uiManager.ShowBattleResult(resultMessage, battleStats.TotalTurns, battleStats.DamageDealt, battleStats.DamageReceived);
            }
            
            // Отключаем все элементы управления
            uiManager.ShowActionButtons(false);
            uiManager.EnablePlayerSelection(false);
            uiManager.EnableEnemySelection(false);
            
            // Останавливаем игру
            isPlayerTurn = false;
            actionsRemaining = 0;
        }
    }

    private bool CheckBattleEnd()
    {
        bool playerDefeated = playerSquad.IsSquadDefeated();
        bool enemyDefeated = enemySquad.IsSquadDefeated();
        return playerDefeated || enemyDefeated;
    }

    public BattleResult DetermineBattleResult()
    {
        bool playerDefeated = playerSquad.IsSquadDefeated();
        bool enemyDefeated = enemySquad.IsSquadDefeated();
        
        if (playerDefeated && enemyDefeated)
            return BattleResult.Draw;
            
        if (playerDefeated)
            return BattleResult.EnemyWin;
            
        if (enemyDefeated)
            return BattleResult.PlayerWin;
            
        return BattleResult.Draw;
    }

    public void OnActionCompleted()
    {
        if (actionsRemaining > 0)
        {
            actionsRemaining--;
            uiManager.UpdateActionsRemaining(actionsRemaining);
        }

        if (actionsRemaining <= 0)
        {
            EndPlayerTurn();
        }
        else
        {
            uiManager.ShowActionButtons(true);
        }

        pendingAction = ActionType.None;
    }

    public ActionType GetCurrentAction()
    {
        return pendingAction;
    }

    public void EndTurnButtonClicked()
    {
        if (isPlayerTurn)
        {
            Debug.Log("Игрок завершает ход досрочно");
            EndPlayerTurn();
        }
    }

    public void OnWeaponSelected()
    {
        weaponSelected = true;
        SelectAction(ActionType.Attack);
    }

    private void OnPlayerDamageDealt(Character dealer, int damage)
    {
        battleStats.AddDamageDealt(damage);
    }

    private void OnPlayerDamageTaken(Character receiver, int damage)
    {
        battleStats.AddDamageReceived(damage);
    }

    public BattleStatistics GetBattleStatistics()
    {
        return battleStats;
    }

    public void ResetState()
    {
        // Сбрасываем все состояния
        isPlayerTurn = true;
        actionsRemaining = 3;
        pendingAction = ActionType.None;
        weaponSelected = false;
        currentTurn = 1;

        // Сбрасываем статистику
        if (battleStats != null)
        {
            battleStats.Reset();
        }
        else
        {
            battleStats = new BattleStatistics();
        }

        // Отписываемся от событий персонажей
        if (playerSquad != null)
        {
            foreach (Character character in playerSquad.GetAliveMembers())
            {
                if (character != null)
                {
                    character.OnDamageDealt -= OnPlayerDamageDealt;
                    character.OnDamageTaken -= OnPlayerDamageTaken;
                }
            }
        }
    }

    // Вспомогательный метод для получения названия типа атаки
    private string GetAttackTypeName(AttackType attackType)
    {
        switch (attackType)
        {
            case AttackType.Sword:
                return "меч";
            case AttackType.Bow:
                return "лук";
            case AttackType.Magic:
                return "магию";
            default:
                return "неизвестное оружие";
        }
    }
}

public enum ActionType
{
    None,
    Attack,
    SuperAttack,
    Move
} 