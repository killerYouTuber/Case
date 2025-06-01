using UnityEngine;

public class PlayerCharacter : Character
{
    [Header("Player Specific")]
    [SerializeField] private int moveRange = 3;
    private int defaultAttackRange = 2;

    private void Awake()
    {
        attackRange = defaultAttackRange; // Устанавливаем радиус атаки
        Debug.Log($"PlayerCharacter.Awake: Установлен радиус атаки {attackRange}");
    }

    private void Start()
    {
        base.Initialize(Team.Player);
    }

    public int GetMoveRange()
    {
        return moveRange;
    }

    public override int GetAttackRange()
    {
        Debug.Log($"PlayerCharacter.GetAttackRange: Возвращаем радиус атаки {attackRange}");
        return attackRange;
    }

    public void SetAttackRange(int range)
    {
        if (range <= 0)
        {
            Debug.LogError($"Попытка установить некорректное значение attackRange: {range}");
            return;
        }
        attackRange = range;
        Debug.Log($"PlayerCharacter.SetAttackRange: Установлена новая дальность атаки {range}");
    }

    public void RestoreDefaultAttackRange()
    {
        attackRange = defaultAttackRange;
        Debug.Log($"PlayerCharacter.RestoreDefaultAttackRange: Восстановлена базовая дальность атаки {defaultAttackRange}");
    }
} 