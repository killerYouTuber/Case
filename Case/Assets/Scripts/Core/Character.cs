using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public enum Team
{
    Player,
    Enemy
}

public enum AttackType
{
    Sword,  // Меч
    Bow,    // Лук
    Magic   // Магия
}

public class Character : MonoBehaviour
{
    [Header("Character Stats")]
    [SerializeField] private string characterName = "Character";
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int maxArmor = 50;
    [SerializeField] private float maxEnergy = 100f;
    [SerializeField] private int baseAttackPower = 10;
    
    [Header("Attack Settings")]
    [SerializeField] private float attackAnimationDuration = 0.5f;

    [Header("Base Stats")]
    [SerializeField] protected int attackPower = 20;
    protected int attackRange = 2;  // Базовый радиус атаки

    [Header("Super Attack")]
    [SerializeField] private int superAttackPower = 40;
    [SerializeField] private float energyGainOnDealingDamage = 35f;  // Энергия за нанесение урона

    [Header("Visual")]
    [SerializeField] private GameObject damageTextPrefab;
    [SerializeField] private Transform damageTextSpawnPoint;
    [SerializeField] private GameObject superAttackEffect;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color playerColor = new Color(0f, 0f, 1f, 1f); // Синий, полностью непрозрачный
    [SerializeField] private Color enemyColor = new Color(1f, 0f, 0f, 1f); // Красный, полностью непрозрачный

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f; // Скорость движения персонажа
    [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // Кривая для плавного движения
    [SerializeField] private int maxMoveDistance = 3; // Максимальное расстояние для прямого перемещения

    [Header("Attack Types")]
    private Dictionary<AttackType, (int range, int damage)> attackTypes;
    private AttackType currentAttackType = AttackType.Magic;

    private int currentHealth;
    private float currentEnergy = 0f;
    private int currentArmor;
    private bool isAlive = true;
    private Team team;
    private Rigidbody2D rb;
    
    public event Action<int, int> OnHealthChanged;
    public event Action<float, float> OnEnergyChanged;
    public event Action<int> OnArmorChanged;
    public event Action OnCharacterDied;
    public event Action OnStatsChanged;
    public event Action<Character, int> OnDamageTaken;  // New event for damage tracking
    public event Action<Character, int> OnDamageDealt;  // New event for damage tracking

    private Vector2Int currentPosition;
    private Vector3 targetPosition;
    private bool isMoving = false;
    private Coroutine moveCoroutine;

    private void OnValidate()
    {
        // Убеждаемся, что значения не превышают максимальные
        maxArmor = Mathf.Min(maxArmor, 50);  // Максимальная броня не может быть больше 50
    }

    private void Awake()
    {
        // Рандомизируем начальные характеристики
        maxHealth = UnityEngine.Random.Range(100, 126); // от 100 до 125
        baseAttackPower = UnityEngine.Random.Range(20, 31); // от 20 до 30
        maxArmor = UnityEngine.Random.Range(5, 16); // от 5 до 15

        ResetState();
        
        // Создаем точку спавна текста урона, если она не назначена
        if (damageTextSpawnPoint == null)
        {
            GameObject spawnPoint = new GameObject("DamageTextSpawnPoint");
            spawnPoint.transform.SetParent(transform);
            spawnPoint.transform.localPosition = new Vector3(0f, 2f, 0f);
            damageTextSpawnPoint = spawnPoint.transform;
            Debug.Log($"Created damage text spawn point for {characterName}");
        }

        // Инициализируем типы атак с базовыми значениями
        attackTypes = new Dictionary<AttackType, (int range, int damage)>
        {
            { AttackType.Sword, (1, baseAttackPower + 20) },  // Меч: базовый урон + 20
            { AttackType.Bow, (3, baseAttackPower) },         // Лук: базовый урон
            { AttackType.Magic, (2, baseAttackPower + 10) }   // Магия: базовый урон + 10
        };

        // Устанавливаем базовую атаку равной урону текущего типа атаки
        attackPower = attackTypes[currentAttackType].damage;
        
        Debug.Log($"=== СОЗДАНИЕ ПЕРСОНАЖА {characterName} ===");
        Debug.Log($"Максимальное здоровье: {maxHealth}");
        Debug.Log($"Базовая сила атаки: {baseAttackPower}");
        Debug.Log($"Урон мечом: {attackTypes[AttackType.Sword].damage}");
        Debug.Log($"Урон луком: {attackTypes[AttackType.Bow].damage}");
        Debug.Log($"Урон магией: {attackTypes[AttackType.Magic].damage}");
        Debug.Log($"Максимальная броня: {maxArmor}");
        
        // Настраиваем Rigidbody2D
        SetupRigidbody();
        
        // Настраиваем визуальные компоненты
        SetupVisuals();
        
        // Настраиваем коллайдер
        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<BoxCollider2D>();
        }
        
        // Настраиваем размер коллайдера под размер спрайта
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            collider.size = spriteRenderer.sprite.bounds.size;
            collider.offset = Vector2.zero;
        }
        else
        {
            collider.size = new Vector2(1f, 1f);
            collider.offset = Vector2.zero;
        }

        collider.isTrigger = true;
    }

    private void SetupRigidbody()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
    }

    private void SetupVisuals()
    {
        Debug.Log($"=== НАСТРОЙКА ВИЗУАЛЬНЫХ КОМПОНЕНТОВ ДЛЯ {characterName} ===");
        
        // Проверяем наличие и настраиваем SpriteRenderer
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            Debug.Log("Получен существующий SpriteRenderer");
        }
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            Debug.Log("Создан новый SpriteRenderer");
        }

        // Если у спрайта нет текстуры, создаем временную
        if (spriteRenderer.sprite == null)
        {
            Debug.Log("Спрайт отсутствует, создаем временный");
            CreateTemporarySprite();
        }

        // Устанавливаем размер спрайта
        transform.localScale = Vector3.one;

        // Устанавливаем сортировку (персонажи должны быть поверх сетки)
        spriteRenderer.sortingOrder = 2;
        Debug.Log($"Установлен sortingOrder: {spriteRenderer.sortingOrder}");

        // Проверяем видимость
        Debug.Log($"Проверка видимости спрайта:");
        Debug.Log($"- GameObject active: {gameObject.activeSelf}");
        Debug.Log($"- SpriteRenderer enabled: {spriteRenderer.enabled}");
        Debug.Log($"- Sprite assigned: {(spriteRenderer.sprite != null)}");
        Debug.Log($"- Position: {transform.position}");
        Debug.Log($"- Scale: {transform.localScale}");
        Debug.Log($"- Color: {spriteRenderer.color}");
    }

    private void CreateTemporarySprite()
    {
        int size = 32;
        // Создаем текстуру size x size пикселей
        Texture2D texture = new Texture2D(size, size);
        Color[] colors = new Color[size * size];
        
        // Создаем круг с обводкой
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int index = y * size + x;
                
                // Вычисляем расстояние от центра
                float dx = x - size/2f;
                float dy = y - size/2f;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                
                // Основной круг
                if (distance < size/2f - 2)
                {
                    colors[index] = new Color(1f, 1f, 1f, 1f); // Белый цвет для основного круга
                }
                // Обводка
                else if (distance < size/2f)
                {
                    colors[index] = new Color(0f, 0f, 0f, 1f); // Черная обводка
                }
                else
                {
                    colors[index] = new Color(0f, 0f, 0f, 0f); // Прозрачный фон
                }
            }
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        texture.name = $"{characterName}_TempSprite";

        // Создаем спрайт из текстуры с центральным pivot point
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f), // Нормализованная позиция pivot (всегда в центре)
            32f, // Pixels per unit
            0,
            SpriteMeshType.Tight,
            Vector4.zero // Без отступов
        );
        
        sprite.name = $"{characterName}_TempSprite";
        spriteRenderer.sprite = sprite;
        Debug.Log($"Character {characterName}: Created temporary sprite with centered pivot at {size/2}, {size/2}");
        Debug.Log($"Sprite dimensions: {sprite.rect.width}x{sprite.rect.height}");
        Debug.Log($"Sprite bounds: {sprite.bounds}");
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Отрисовываем границы коллайдера для отладки
        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if (collider != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 center = transform.position + (Vector3)collider.offset;
            Vector3 size = collider.size;
            Gizmos.DrawWireCube(center, size);
        }
    }
#endif

    private void Start()
    {
        // Проверяем начальную позицию
        Debug.Log($"Character {characterName} initialized at position: {transform.position}");
    }

    public void Initialize(Team team)
    {
        Debug.Log($"=== НАЧАЛО ИНИЦИАЛИЗАЦИИ ПЕРСОНАЖА {characterName} ===");
        
        this.team = team;
        
        // Настраиваем визуальные компоненты
        SetupVisuals();
        
        // Проверяем, что SpriteRenderer и спрайт установлены правильно
        if (spriteRenderer == null)
        {
            Debug.LogError($"Character {characterName}: SpriteRenderer is null after initialization!");
            return;
        }
        
        if (spriteRenderer.sprite == null)
        {
            Debug.LogWarning($"Character {characterName}: Sprite is null after initialization, creating temporary sprite");
            CreateTemporarySprite();
        }
        
        // Устанавливаем цвет в зависимости от команды
        Color teamColor = (team == Team.Player) ? playerColor : enemyColor;
        spriteRenderer.color = teamColor;
        Debug.Log($"Character {characterName}: Set team color to {teamColor} (Alpha: {teamColor.a})");
        
        // Проверяем видимость после установки цвета
        Debug.Log($"Character {characterName}: Sprite visibility check:");
        Debug.Log($"- GameObject active: {gameObject.activeSelf}");
        Debug.Log($"- SpriteRenderer enabled: {spriteRenderer.enabled}");
        Debug.Log($"- Sprite assigned: {(spriteRenderer.sprite != null)}");
        Debug.Log($"- Color: {spriteRenderer.color}");
        Debug.Log($"- Position: {transform.position}");
        Debug.Log($"- Scale: {transform.localScale}");
        Debug.Log($"- Sorting Order: {spriteRenderer.sortingOrder}");
        
        // Убеждаемся, что объект активен и спрайт включен
        if (!gameObject.activeSelf)
        {
            Debug.LogWarning($"Character {characterName}: GameObject was inactive, activating");
            gameObject.SetActive(true);
        }
        if (!spriteRenderer.enabled)
        {
            Debug.LogWarning($"Character {characterName}: SpriteRenderer was disabled, enabling");
            spriteRenderer.enabled = true;
        }
        
        // Проверяем и устанавливаем начальные значения
        currentHealth = maxHealth;
        currentArmor = maxArmor;
        currentEnergy = 0f;
        
        Debug.Log($"=== СТАТУС ИНИЦИАЛИЗАЦИИ ПЕРСОНАЖА {characterName} ===");
        Debug.Log($"Максимальное здоровье: {maxHealth}");
        Debug.Log($"Текущее здоровье: {currentHealth}");
        Debug.Log($"Сила атаки: {attackPower}");
        Debug.Log($"Максимальная броня: {maxArmor}");
        Debug.Log($"Текущая броня: {currentArmor}");
        Debug.Log($"Энергия: {currentEnergy}/{maxEnergy}");
        Debug.Log($"Спрайт установлен: {spriteRenderer.sprite != null}");
        Debug.Log($"Цвет спрайта: {spriteRenderer.color}");
        Debug.Log($"Позиция объекта: {transform.position}");
        Debug.Log($"Объект активен: {gameObject.activeSelf}");
        Debug.Log($"SpriteRenderer включен: {spriteRenderer.enabled}");
        
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnArmorChanged?.Invoke(currentArmor);
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
        
        Debug.Log($"=== ЗАВЕРШЕНИЕ ИНИЦИАЛИЗАЦИИ ПЕРСОНАЖА {characterName} ===");
    }

    public bool CanPerformSuperAttack()
    {
        bool hasEnoughEnergy = currentEnergy >= maxEnergy;
        Debug.Log($"{characterName}: Проверка возможности использования супер-атаки. Энергия: {currentEnergy}/{maxEnergy}, Достаточно энергии: {hasEnoughEnergy}, Жив: {isAlive}");
        return isAlive && hasEnoughEnergy;
    }

    public bool IsAlive()
    {
        return isAlive;
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    public int GetMaxHealth()
    {
        return maxHealth;
    }

    public float GetCurrentEnergy()
    {
        return currentEnergy;
    }

    public float GetMaxEnergy()
    {
        return maxEnergy;
    }

    public Team GetTeam()
    {
        return team;
    }

    public string GetCharacterName()
    {
        return characterName;
    }

    public void TakeDamage(int damage, bool gainEnergy = true)
    {
        if (!isAlive) return;

        Debug.Log($"{characterName} получает {damage} урона");

        // Вычитаем броню из входящего урона
        int finalDamage = damage - currentArmor;
        // Если урон после брони меньше 1, устанавливаем его в 1
        if (finalDamage < 1) finalDamage = 1;
        
        Debug.Log($"{characterName}: Броня ({currentArmor}) поглотила {damage - finalDamage} урона, итоговый урон: {finalDamage}");
        
        // Наносим урон по здоровью
        currentHealth = Mathf.Max(0, currentHealth - finalDamage);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnDamageTaken?.Invoke(this, finalDamage);  // Trigger damage taken event
        Debug.Log($"{characterName}: Осталось здоровья: {currentHealth}/{maxHealth}");

        // Показываем текст с уроном
        if (damageTextPrefab != null && damageTextSpawnPoint != null)
        {
            // Создаем текст урона в мировых координатах
            GameObject damageTextObj = Instantiate(damageTextPrefab, damageTextSpawnPoint.position, Quaternion.identity);
            DamageText damageText = damageTextObj.GetComponent<DamageText>();
            if (damageText != null)
            {
                damageText.SetText(finalDamage.ToString());
                damageText.SetColor(Color.red);
            }
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        int previousHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        
        // Spawn heal text
        if (damageTextPrefab != null && damageTextSpawnPoint != null)
        {
            // Создаем текст лечения как дочерний объект Canvas
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                GameObject healTextObj = Instantiate(damageTextPrefab, canvas.transform);
                DamageText healText = healTextObj.GetComponent<DamageText>();
                if (healText != null)
                {
                    healText.SetText("+" + (currentHealth - previousHealth).ToString());
                    healText.SetColor(Color.green);
                    // Устанавливаем мировую позицию для текста лечения
                    healText.SetWorldPosition(damageTextSpawnPoint.position);
                }
            }
        }
        
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void HealPercentage(float percentage)
    {
        int healAmount = Mathf.RoundToInt(maxHealth * percentage / 100f);
        Heal(healAmount);
    }

    public void IncreaseEnergy(float amount)
    {
        currentEnergy = Mathf.Min(maxEnergy, currentEnergy + amount);
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
    }

    public void ResetEnergy()
    {
        currentEnergy = 0f;
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
    }

    public void ModifyAttackPower(int modifier)
    {
        // Добавляем модификатор к каждому типу атаки напрямую
        foreach (AttackType type in System.Enum.GetValues(typeof(AttackType)))
        {
            if (attackTypes.ContainsKey(type))
            {
                var attackData = attackTypes[type];
                int originalDamage = attackData.damage;
                attackData.damage = originalDamage + modifier;  // Просто добавляем модификатор
                attackTypes[type] = attackData;
                
                Debug.Log($"Тип атаки {type}: базовый урон {originalDamage} -> новый урон {attackData.damage}");
            }
        }
        
        // Обновляем базовую атаку текущего типа
        attackPower = attackTypes[currentAttackType].damage;
        
        OnStatsChanged?.Invoke();
        Debug.Log($"{characterName} attack modified by {modifier}. New base attack: {attackPower}");
    }

    public int GetAttackPower()
    {
        return attackPower;
    }

    public int GetSuperAttackPower()
    {
        return superAttackPower;
    }

    public void PerformAttack(Character target)
    {
        if (target == null || !target.IsAlive())
        {
            Debug.LogWarning($"{characterName}: Cannot attack - target is null or already dead");
            return;
        }

        Debug.Log($"{characterName} атакует {target.GetCharacterName()}");
        
        // Получаем урон текущего типа атаки
        int attackDamage = attackTypes[currentAttackType].damage;
        Debug.Log($"Атака с силой: {attackDamage} (тип атаки: {currentAttackType})");
        
        // Наносим урон (броня цели поглотит часть урона в методе TakeDamage)
        target.TakeDamage(attackDamage);
        OnDamageDealt?.Invoke(this, attackDamage);  // Trigger damage dealt event
        
        // Увеличиваем энергию за нанесение урона
        IncreaseEnergy(energyGainOnDealingDamage);
        Debug.Log($"Энергия увеличена на {energyGainOnDealingDamage}. Текущее значение: {currentEnergy}/{maxEnergy}");
    }

    public void PerformSuperAttack(Character target)
    {
        if (!CanPerformSuperAttack() || target == null || !target.IsAlive())
        {
            Debug.Log($"{characterName}: Не удалось выполнить супер-атаку. CanPerformSuperAttack: {CanPerformSuperAttack()}, Цель существует: {target != null}, Цель жива: {target?.IsAlive()}");
            return;
        }

        Debug.Log($"{characterName} использует супер-атаку на {target.GetCharacterName()}");

        // Сбрасываем энергию в начале метода
        ResetEnergy();
        Debug.Log($"Энергия сброшена перед атакой. Текущее значение: {currentEnergy}/{maxEnergy}");

        // Show super attack effect
        if (superAttackEffect != null)
        {
            GameObject effect = Instantiate(superAttackEffect, target.transform.position, Quaternion.identity);
            Destroy(effect, 1f); // Уничтожаем эффект через 1 секунду
        }

        // Наносим фиксированный урон 40 единиц
        target.TakeDamage(40, false);
        
        // Восстанавливаем 20 HP атакующему
        Heal(20);
    }

    public int GetAttackDamage()
    {
        return attackPower;
    }

    public int GetSuperAttackDamage()
    {
        return superAttackPower;
    }

    public void ConsumeSuperAttackEnergy()
    {
        if (CanPerformSuperAttack())
        {
            ResetEnergy();
        }
    }

    private void Die()
    {
        isAlive = false;
        OnCharacterDied?.Invoke();
        
        // Проверяем состояние игры сразу после смерти персонажа
        BattleSystem battleSystem = FindAnyObjectByType<BattleSystem>();
        if (battleSystem != null)
        {
            battleSystem.CheckAndHandleGameOver();
        }
        
        // Отключаем объект персонажа
        gameObject.SetActive(false);
    }

    public void SetPosition(Vector2Int newPosition)
    {
        currentPosition = newPosition;
        // Обновляем позицию с учетом размера клетки
        transform.position = GridManager.Instance.GetWorldPosition(newPosition);
    }

    public bool IsMoving()
    {
        return isMoving;
    }

    public Vector2Int GetPosition()
    {
        return currentPosition;
    }

    public void OnMoveButtonClick()
    {
        GridManager.Instance.StartMoveMode(this);
    }

    public bool TryMoveTo(Vector2Int targetPos)
    {
        if (isMoving)
        {
            return false;
        }

        Vector2Int currentPos = GetPosition();
        
        // Проверяем, что движение происходит только по прямой (горизонтально или вертикально)
        bool isHorizontalMove = currentPos.y == targetPos.y;
        bool isVerticalMove = currentPos.x == targetPos.x;
        
        if (!isHorizontalMove && !isVerticalMove)
        {
            Debug.Log("Движение возможно только по горизонтали или вертикали");
            return false;
        }
        
        // Вычисляем расстояние до цели
        int distance = Mathf.Abs(targetPos.x - currentPos.x) + Mathf.Abs(targetPos.y - currentPos.y);
        
        // Проверяем, что расстояние не превышает 3 клеток
        if (distance > maxMoveDistance)
        {
            Debug.Log($"Слишком большое расстояние: {distance}. Максимум: {maxMoveDistance}");
            return false;
        }
        
        // Определяем направление движения
        Vector2Int direction = new Vector2Int(
            isHorizontalMove ? Mathf.Clamp(targetPos.x - currentPos.x, -1, 1) : 0,
            isVerticalMove ? Mathf.Clamp(targetPos.y - currentPos.y, -1, 1) : 0
        );
        
        // Строим путь до целевой клетки
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int currentPathPos = currentPos;
        
        for (int step = 0; step < distance; step++)
        {
            Vector2Int nextPos = currentPathPos + direction;
            
            // Проверяем, можно ли двигаться дальше
            if (!GridManager.Instance.IsValidPosition(nextPos) || GridManager.Instance.IsOccupied(nextPos))
            {
                Debug.Log($"Путь заблокирован на шаге {step + 1}");
                break;
            }
            
            path.Add(nextPos);
            currentPathPos = nextPos;
        }
        
        // Если есть путь, начинаем движение
        if (path.Count > 0)
        {
            Debug.Log($"Начинаем движение по пути из {path.Count} клеток");
            moveCoroutine = StartCoroutine(MoveThroughPath(path));
            return true;
        }
        
        Debug.Log("Не удалось построить путь");
        return false;
    }

    private IEnumerator MoveThroughPath(List<Vector2Int> path)
    {
        isMoving = true;
        
        // Получаем начальную и конечную позиции
        Vector3 startPos = transform.position;
        Vector3 endPos = GridManager.Instance.GetWorldPosition(path[path.Count - 1]);
        float totalDistance = Vector3.Distance(startPos, endPos);
        float elapsedTime = 0f;
        
        // Вычисляем время движения на основе расстояния и скорости
        float moveDuration = totalDistance / moveSpeed;
        Debug.Log($"Moving character {characterName} - Distance: {totalDistance}, Speed: {moveSpeed}, Duration: {moveDuration}");
        
        while (elapsedTime < moveDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / moveDuration;
            
            // Используем кривую анимации для более плавного движения
            float curveT = movementCurve.Evaluate(t);
            
            // Добавляем небольшое вертикальное движение для эффекта "прыжка"
            float height = Mathf.Sin(t * Mathf.PI) * 0.2f;
            
            // Находим текущую позицию на пути
            Vector3 currentPos = Vector3.Lerp(startPos, endPos, curveT);
            currentPos.y += height;
            
            transform.position = currentPos;
            yield return null;
        }
        
        // Убеждаемся, что персонаж точно встал на конечную позицию
        transform.position = endPos;
        currentPosition = path[path.Count - 1];
        
        // Обновляем позицию в GridManager
        GridManager.Instance.UpdateCharacterPosition(this, currentPosition);
        
        isMoving = false;
        moveCoroutine = null;
        
        Debug.Log($"Character {characterName} finished moving to position {currentPosition}");
    }

    // Метод для отмены текущего движения
    public void CancelMovement()
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
            isMoving = false;
        }
    }

    // Метод для врагов, чтобы двигаться прямо в указанном направлении
    public bool MoveInDirection(Vector2Int direction)
    {
        if (team != Team.Enemy) return false;
        return TryMoveTo(direction);
    }

    public virtual int GetAttackRange()
    {
        return attackTypes[currentAttackType].range;
    }

    public int GetCurrentArmor()
    {
        return currentArmor;
    }

    public int GetMaxArmor()
    {
        return maxArmor;
    }

    public void RestoreArmor(int amount)
    {
        if (!isAlive) return;
        
        currentArmor = Mathf.Min(maxArmor, currentArmor + amount);
        OnArmorChanged?.Invoke(currentArmor);
    }

    public void ModifyArmor(int modifier)
    {
        maxArmor += modifier;
        if (modifier > 0)
        {
            currentArmor += modifier;  // Also increase current armor when adding armor
        }
        currentArmor = Mathf.Min(currentArmor, maxArmor);  // Ensure current armor doesn't exceed max
        OnArmorChanged?.Invoke(currentArmor);
    }

    public void ModifyHealth(int amount)
    {
        int newHealth = currentHealth + amount;
        currentHealth = Mathf.Clamp(newHealth, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log($"{characterName} health modified by {amount}. New health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void ModifyEnergy(float amount)
    {
        float newEnergy = currentEnergy + amount;
        currentEnergy = Mathf.Clamp(newEnergy, 0f, maxEnergy);
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
        Debug.Log($"{characterName} energy modified by {amount}. New energy: {currentEnergy}/{maxEnergy}");
    }

    public void SetAttackType(AttackType type)
    {
        currentAttackType = type;
        var (range, damage) = attackTypes[type];
        attackRange = range;
        attackPower = damage;
        OnStatsChanged?.Invoke();
        
        Debug.Log($"{characterName} сменил тип атаки на {type}. Новый радиус: {range}, Новый урон: {damage}");
    }

    public AttackType GetCurrentAttackType()
    {
        return currentAttackType;
    }

    public void ResetState()
    {
        Debug.Log($"=== СБРОС СОСТОЯНИЯ ПЕРСОНАЖА {characterName} ===");
        
        // Сбрасываем здоровье
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        
        // Сбрасываем броню
        currentArmor = maxArmor;
        OnArmorChanged?.Invoke(currentArmor);
        
        // Сбрасываем энергию
        currentEnergy = 0f;
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
        
        // Сбрасываем тип атаки на значение по умолчанию
        currentAttackType = AttackType.Magic;
        
        // Восстанавливаем базовые значения атак
        attackTypes = new Dictionary<AttackType, (int range, int damage)>
        {
            { AttackType.Sword, (1, baseAttackPower + 20) },  // Меч: базовый урон + 20
            { AttackType.Bow, (3, baseAttackPower) },         // Лук: базовый урон
            { AttackType.Magic, (2, baseAttackPower + 10) }   // Магия: базовый урон + 10
        };
        
        // Устанавливаем базовую атаку равной урону текущего типа атаки
        attackPower = attackTypes[currentAttackType].damage;
        
        // Сбрасываем флаги
        isAlive = true;
        isMoving = false;
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }
        
        // Активируем объект
        gameObject.SetActive(true);
        
        // Вызываем событие обновления статов
        OnStatsChanged?.Invoke();
        
        Debug.Log($"Персонаж {characterName} сброшен к начальному состоянию:");
        Debug.Log($"- Здоровье: {currentHealth}/{maxHealth}");
        Debug.Log($"- Броня: {currentArmor}/{maxArmor}");
        Debug.Log($"- Энергия: {currentEnergy}/{maxEnergy}");
        Debug.Log($"- Тип атаки: {currentAttackType}");
        Debug.Log($"- Базовая сила атаки: {baseAttackPower}");
        Debug.Log($"- Урон мечом: {attackTypes[AttackType.Sword].damage}");
        Debug.Log($"- Урон луком: {attackTypes[AttackType.Bow].damage}");
        Debug.Log($"- Урон магией: {attackTypes[AttackType.Magic].damage}");
    }

    private IEnumerator PlayAttackAnimation()
    {
        // Здесь можно добавить визуальные эффекты атаки
        // Например, мигание спрайта или изменение цвета
        if (spriteRenderer != null)
        {
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = Color.yellow; // Подсветка при атаке
            yield return new WaitForSeconds(attackAnimationDuration * 0.5f);
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(attackAnimationDuration * 0.5f);
        }
        else
        {
            yield return new WaitForSeconds(attackAnimationDuration);
        }
    }
} 