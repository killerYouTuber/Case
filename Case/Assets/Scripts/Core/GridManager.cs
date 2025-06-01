using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Grid Settings")]
    [SerializeField] private int gridWidth = 10;
    [SerializeField] private int gridHeight = 10;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private GameObject cellPrefab;
    
    [Header("References")]
    [SerializeField] private UIManager uiManager;
    [SerializeField] private BattleSystem battleSystem;
    
    [Header("Visual Settings")]
    [SerializeField] private Color defaultCellColor = new Color(1f, 1f, 1f, 0.2f);
    [SerializeField] private Color moveRangeColor = new Color(0f, 1f, 0f, 0.3f);
    [SerializeField] private Color attackRangeColor = new Color(1f, 0f, 0f, 0.3f);

    public Color DefaultCellColor => defaultCellColor;
    public Color MoveRangeColor => moveRangeColor;
    public Color AttackRangeColor => attackRangeColor;
    
    private GridCell[,] gridCells;
    private List<GridCell> highlightedCells = new List<GridCell>();
    private Character selectedCharacter;
    private bool isInMoveMode = false;
    private Dictionary<Vector2Int, Character> characterPositions = new Dictionary<Vector2Int, Character>();
    private bool isInAttackMode = false;
    private Character attackingCharacter = null;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Центрируем сетку в начале координат
        transform.position = Vector3.zero;

        // Находим необходимые компоненты, если они не были назначены
        if (uiManager == null)
        {
            uiManager = FindAnyObjectByType<UIManager>();
        }
        if (battleSystem == null)
        {
            battleSystem = FindAnyObjectByType<BattleSystem>();
        }
    }

    private void Start()
    {
        CreateGrid();
        
        // Ждем один кадр, чтобы убедиться, что все компоненты инициализированы
        StartCoroutine(InitializeCharactersNextFrame());
    }

    private System.Collections.IEnumerator InitializeCharactersNextFrame()
    {
        yield return null; // Ждем один кадр
        PlaceCharactersRandomly();
    }

    private void CreateGrid()
    {
        Debug.Log($"Creating grid with size {gridWidth}x{gridHeight}, cell size: {cellSize}");
        gridCells = new GridCell[gridWidth, gridHeight];
        
        // Создаем родительский объект для клеток
        GameObject gridHolder = new GameObject("GridCells");
        gridHolder.transform.SetParent(transform);
        
        // Вычисляем смещение для центрирования сетки
        float offsetX = -((gridWidth * cellSize) / 2f);
        float offsetY = -((gridHeight * cellSize) / 2f);
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector3 position = new Vector3(
                    (x * cellSize) + offsetX + (cellSize / 2f),
                    (y * cellSize) + offsetY + (cellSize / 2f),
                    0
                );
                
                GameObject cellObject = Instantiate(cellPrefab, position, Quaternion.identity, gridHolder.transform);
                cellObject.name = $"Cell_{x}_{y}";
                
                // Устанавливаем размер клетки
                cellObject.transform.localScale = new Vector3(cellSize, cellSize, 1);
                
                GridCell cell = cellObject.GetComponent<GridCell>();
                if (cell == null)
                {
                    Debug.LogError($"Cell prefab does not have GridCell component!");
                    continue;
                }
                
                // Добавляем BoxCollider2D для обработки кликов
                BoxCollider2D collider = cellObject.GetComponent<BoxCollider2D>();
                if (collider == null)
                {
                    collider = cellObject.AddComponent<BoxCollider2D>();
                }
                collider.size = Vector2.one; // Размер 1x1 для точного попадания
                
                cell.SetGridPosition(new Vector2Int(x, y));
                cell.SetColor(defaultCellColor);
                
                gridCells[x, y] = cell;
                
                Debug.Log($"Created cell at grid position ({x}, {y}), world position: {position}");
            }
        }
        
        Debug.Log("Grid creation completed!");
    }

    private void PlaceCharactersRandomly()
    {
        Debug.Log("=== НАЧАЛО РАЗМЕЩЕНИЯ ПЕРСОНАЖЕЙ ===");
        
        if (battleSystem == null)
        {
            Debug.LogError("BattleSystem reference is missing in GridManager!");
            return;
        }

        Squad playerSquad = battleSystem.GetPlayerSquad();
        Squad enemySquad = battleSystem.GetEnemySquad();

        if (playerSquad == null || enemySquad == null)
        {
            Debug.LogError("Squads not found in BattleSystem!");
            return;
        }

        Character playerCharacter = playerSquad.GetActiveCharacter();
        Character enemyCharacter = enemySquad.GetActiveCharacter();

        if (playerCharacter == null || enemyCharacter == null)
        {
            Debug.LogError("Active characters not found in squads!");
            return;
        }

        Debug.Log($"Найдены персонажи для размещения:");
        Debug.Log($"- Игрок: {playerCharacter.GetCharacterName()}");
        Debug.Log($"- Противник: {enemyCharacter.GetCharacterName()}");

        // Размещаем игрока
        Vector2Int playerPos = GetRandomEmptyPosition();
        Debug.Log($"Выбрана позиция для игрока: {playerPos}");
        Debug.Log($"Мировая позиция для игрока: {GetWorldPosition(playerPos)}");
        
        PlaceCharacter(playerCharacter, playerPos);
        Debug.Log($"Игрок размещен. Текущая позиция: {playerCharacter.transform.position}");
        Debug.Log($"Состояние игрока:");
        Debug.Log($"- GameObject active: {playerCharacter.gameObject.activeSelf}");
        Debug.Log($"- SpriteRenderer enabled: {playerCharacter.GetComponent<SpriteRenderer>().enabled}");
        Debug.Log($"- Position in grid: {playerCharacter.GetPosition()}");

        // Размещаем противника
        Vector2Int enemyPos;
        do
        {
            enemyPos = GetRandomEmptyPosition();
        } while (enemyPos == playerPos);
        
        Debug.Log($"Выбрана позиция для противника: {enemyPos}");
        Debug.Log($"Мировая позиция для противника: {GetWorldPosition(enemyPos)}");
        
        PlaceCharacter(enemyCharacter, enemyPos);
        Debug.Log($"Противник размещен. Текущая позиция: {enemyCharacter.transform.position}");
        Debug.Log($"Состояние противника:");
        Debug.Log($"- GameObject active: {enemyCharacter.gameObject.activeSelf}");
        Debug.Log($"- SpriteRenderer enabled: {enemyCharacter.GetComponent<SpriteRenderer>().enabled}");
        Debug.Log($"- Position in grid: {enemyCharacter.GetPosition()}");

        Debug.Log("=== ЗАВЕРШЕНИЕ РАЗМЕЩЕНИЯ ПЕРСОНАЖЕЙ ===");
        
        // Проверяем состояние сетки после размещения
        Debug.Log("Проверка состояния сетки:");
        foreach (var pos in characterPositions)
        {
            Debug.Log($"Клетка {pos.Key}: {pos.Value.GetCharacterName()}");
        }
    }

    private Vector2Int GetRandomEmptyPosition()
    {
        List<Vector2Int> emptyPositions = new List<Vector2Int>();
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (IsPositionEmpty(pos))
                {
                    emptyPositions.Add(pos);
                }
            }
        }

        if (emptyPositions.Count == 0)
        {
            Debug.LogError("No empty positions on the grid!");
            return Vector2Int.zero;
        }

        return emptyPositions[Random.Range(0, emptyPositions.Count)];
    }

    private bool IsPositionEmpty(Vector2Int pos)
    {
        if (!IsValidPosition(pos)) return false;
        
        // Проверяем, нет ли на клетке других персонажей
        Collider2D[] colliders = Physics2D.OverlapPointAll(GetWorldPosition(pos));
        foreach (var collider in colliders)
        {
            if (collider.GetComponent<Character>() != null)
            {
                return false;
            }
        }
        
        return true;
    }

    public void StartMoveMode(Character character)
    {
        selectedCharacter = character;
        isInMoveMode = true;
        HighlightMoveRange(character.GetPosition(), 3); // 3 клетки - максимальная дистанция
    }

    public void CancelMoveMode()
    {
        isInMoveMode = false;
        ClearHighlights();
        selectedCharacter = null;
    }

    private void HighlightMoveRange(Vector2Int position, int range)
    {
        ClearHighlights();
        Debug.Log($"Подсвечиваем клетки для движения от позиции {position} с дальностью {range}");

        // Подсвечиваем клетки только по прямым линиям (горизонталь и вертикаль)
        // Вправо
        for (int x = 1; x <= range; x++)
        {
            Vector2Int pos = new Vector2Int(position.x + x, position.y);
            if (!HighlightCellIfValid(pos)) break; // Прекращаем подсветку при встрече препятствия
        }

        // Влево
        for (int x = 1; x <= range; x++)
        {
            Vector2Int pos = new Vector2Int(position.x - x, position.y);
            if (!HighlightCellIfValid(pos)) break;
        }

        // Вверх
        for (int y = 1; y <= range; y++)
        {
            Vector2Int pos = new Vector2Int(position.x, position.y + y);
            if (!HighlightCellIfValid(pos)) break;
        }

        // Вниз
        for (int y = 1; y <= range; y++)
        {
            Vector2Int pos = new Vector2Int(position.x, position.y - y);
            if (!HighlightCellIfValid(pos)) break;
        }
    }

    private bool HighlightCellIfValid(Vector2Int pos)
    {
        if (IsValidPosition(pos) && IsPositionEmpty(pos))
        {
            GridCell cell = gridCells[pos.x, pos.y];
            cell.SetColor(moveRangeColor);
            cell.isHighlighted = true;
            highlightedCells.Add(cell);
            return true; // Клетка свободна, можно продолжать в этом направлении
        }
        return false; // Клетка занята или невалидна, нужно прекратить подсветку в этом направлении
    }

    private void ClearHighlights()
    {
        foreach (var cell in highlightedCells)
        {
            if (cell != null)
            {
                cell.SetColor(defaultCellColor);
                cell.isHighlighted = false;
            }
        }
        highlightedCells.Clear();
    }

    public void StartAttackMode(Character attacker)
    {
        if (attacker == null)
        {
            Debug.LogError("StartAttackMode: attacker is null!");
            return;
        }

        Debug.Log($"Entering attack mode with character: {attacker.GetCharacterName()}");
        attackingCharacter = attacker;
        isInAttackMode = true;
        ShowAttackRange(attacker);
    }

    private void ShowAttackRange(Character attacker)
    {
        ClearHighlights();
        
        Vector2Int attackerPos = attacker.GetPosition();
        int attackRange = attacker.GetAttackRange();
        
        Debug.Log($"Showing attack range for {attacker.GetCharacterName()} at {attackerPos} with range {attackRange}");

        // Подсвечиваем все клетки в пределах дальности атаки (манхэттенское расстояние)
        for (int x = -attackRange; x <= attackRange; x++)
        {
            for (int y = -attackRange; y <= attackRange; y++)
            {
                int distance = Mathf.Abs(x) + Mathf.Abs(y);
                if (distance > 0 && distance <= attackRange) // distance > 0 чтобы исключить клетку самого атакующего
                {
                    Vector2Int targetPos = new Vector2Int(attackerPos.x + x, attackerPos.y + y);
                    if (IsValidPosition(targetPos))
                    {
                        GridCell cell = gridCells[targetPos.x, targetPos.y];
                        cell.SetColor(attackRangeColor);
                        cell.isHighlighted = true;
                        highlightedCells.Add(cell);
                    }
                }
            }
        }
    }

    public void OnCellClicked(GridCell cell)
    {
        if (cell == null) return;

        Vector2Int clickedPos = cell.GetGridPosition();
        Debug.Log($"Cell clicked at position: {clickedPos}");

        if (isInMoveMode)
        {
            HandleMoveClick(clickedPos);
        }
        else if (isInAttackMode)
        {
            HandleAttackClick(clickedPos);
        }
    }

    private void HandleAttackClick(Vector2Int targetPos)
    {
        if (attackingCharacter == null)
        {
            Debug.LogError("HandleAttackClick: No attacking character selected!");
            return;
        }

        Debug.Log($"Handling attack click at position: {targetPos}");

        // Проверяем, есть ли противник в кликнутой клетке
        Character targetCharacter = GetCharacterAtPosition(targetPos);
        if (targetCharacter == null)
        {
            Debug.Log("No character at target position");
            return;
        }

        // Проверяем, что цель - противник
        if (targetCharacter.GetTeam() == attackingCharacter.GetTeam())
        {
            Debug.Log("Cannot attack ally!");
            return;
        }

        // Проверяем расстояние до цели
        Vector2Int attackerPos = attackingCharacter.GetPosition();
        int distance = Mathf.Abs(targetPos.x - attackerPos.x) + Mathf.Abs(targetPos.y - attackerPos.y);
        int attackRange = attackingCharacter.GetAttackRange();

        Debug.Log($"Distance to target: {distance}, Attack range: {attackRange}");

        if (distance <= attackRange)
        {
            Debug.Log($"Attacking {targetCharacter.GetCharacterName()} at position {targetPos}");
            
            // Проверяем, можно ли выполнить супер атаку
            if (attackingCharacter.CanPerformSuperAttack() && battleSystem.GetCurrentAction() == ActionType.SuperAttack)
            {
                attackingCharacter.PerformSuperAttack(targetCharacter);
            }
            else
            {
                attackingCharacter.PerformAttack(targetCharacter);
            }
            
            // Завершаем ход
            if (battleSystem != null)
            {
                battleSystem.OnActionCompleted();
            }
            
            // Очищаем состояние атаки
            CancelAttackMode();
        }
        else
        {
            Debug.Log($"Target is too far! Distance: {distance}, Range: {attackRange}");
        }
    }

    public void CancelAttackMode()
    {
        Debug.Log("Canceling attack mode");
        isInAttackMode = false;
        attackingCharacter = null;
        ClearHighlights();
    }

    public bool IsValidPosition(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < gridWidth && pos.y >= 0 && pos.y < gridHeight;
    }

    public Vector3 GetWorldPosition(Vector2Int gridPosition)
    {
        float offsetX = -((gridWidth * cellSize) / 2f);
        float offsetY = -((gridHeight * cellSize) / 2f);
        
        return new Vector3(
            (gridPosition.x * cellSize) + offsetX + (cellSize / 2f),
            (gridPosition.y * cellSize) + offsetY + (cellSize / 2f),
            0
        );
    }

    public void PlaceCharacter(Character character, Vector2Int position)
    {
        if (character == null)
        {
            Debug.LogError("PlaceCharacter: character is null!");
            return;
        }

        if (!IsValidPosition(position))
        {
            Debug.LogError($"Invalid position for character placement: {position}");
            return;
        }

        // Проверяем состояние персонажа
        if (!character.gameObject.activeSelf)
        {
            Debug.LogWarning($"Character {character.GetCharacterName()} was inactive, activating");
            character.gameObject.SetActive(true);
        }

        SpriteRenderer spriteRenderer = character.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            Debug.LogError($"Character {character.GetCharacterName()} has no sprite renderer or sprite!");
            return;
        }

        // Удаляем старую позицию персонажа из словаря
        Vector2Int oldPos = character.GetPosition();
        if (characterPositions.ContainsKey(oldPos) && characterPositions[oldPos] == character)
        {
            characterPositions.Remove(oldPos);
        }

        // Добавляем новую позицию
        characterPositions[position] = character;

        Debug.Log($"Placing character {character.GetCharacterName()} at grid position: {position}");
        Vector3 worldPosition = GetWorldPosition(position);
        Debug.Log($"World position for placement: {worldPosition}");

        // Обновляем позицию персонажа
        character.SetPosition(position);
        character.transform.position = worldPosition;

        // Проверяем, что персонаж действительно переместился
        if (Vector3.Distance(character.transform.position, worldPosition) > 0.01f)
        {
            Debug.LogWarning($"Character position mismatch! Expected: {worldPosition}, Actual: {character.transform.position}");
            character.transform.position = worldPosition;
        }

        Debug.Log($"Character {character.GetCharacterName()} placed successfully. Active: {character.gameObject.activeSelf}, Position: {character.transform.position}, Sprite visible: {spriteRenderer.enabled}");
    }

    public bool IsOccupied(Vector2Int pos)
    {
        return !IsPositionEmpty(pos);
    }
    
    public void UpdateCharacterPosition(Character character, Vector2Int newPosition)
    {
        if (!IsValidPosition(newPosition)) return;
        
        // Удаляем старую позицию
        Vector2Int oldPos = character.GetPosition();
        if (characterPositions.ContainsKey(oldPos) && characterPositions[oldPos] == character)
        {
            characterPositions.Remove(oldPos);
        }

        // Добавляем новую позицию
        characterPositions[newPosition] = character;
        character.SetPosition(newPosition);

        if (battleSystem != null)
        {
            battleSystem.OnCharacterMoved(character);
        }
    }

    private void HandleMoveClick(Vector2Int clickedPos)
    {
        if (selectedCharacter == null)
        {
            Debug.LogError("HandleMoveClick: No character selected for movement!");
            return;
        }

        // Проверяем, что клетка подсвечена (доступна для движения)
        if (gridCells[clickedPos.x, clickedPos.y].isHighlighted)
        {
            Debug.Log($"Moving character to position: {clickedPos}");
            selectedCharacter.TryMoveTo(clickedPos);
            
            if (uiManager != null)
            {
                uiManager.OnGridCellClicked(clickedPos);
            }
        }
        else
        {
            Debug.Log("Selected cell is not in move range");
        }
        
        CancelMoveMode();
    }

    private Character GetCharacterAtPosition(Vector2Int position)
    {
        if (characterPositions.TryGetValue(position, out Character character))
        {
            Debug.Log($"GetCharacterAtPosition: Найден персонаж {character.GetCharacterName()} в позиции {position}");
            return character;
        }
        Debug.Log($"GetCharacterAtPosition: Персонаж не найден в позиции {position}");
        return null;
    }
} 