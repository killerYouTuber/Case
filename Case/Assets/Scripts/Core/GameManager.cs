using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    [SerializeField] private int maxTurns = 10;
    [SerializeField] private float battleStartDelay = 1.0f;

    [Header("References")]
    [SerializeField] private BattleSystem battleSystem;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private CardManager cardManager;

    private int currentTurn = 0;
    private bool isBattleActive = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        Debug.Log("=== ИНИЦИАЛИЗАЦИЯ ИГРЫ ===");
        
        // Находим необходимые компоненты, если они не назначены
        if (battleSystem == null)
            battleSystem = FindAnyObjectByType<BattleSystem>();
        if (uiManager == null)
            uiManager = FindAnyObjectByType<UIManager>();
        if (cardManager == null)
            cardManager = FindAnyObjectByType<CardManager>();

        // Проверяем, что все необходимые компоненты найдены
        if (battleSystem == null || uiManager == null || cardManager == null)
        {
            Debug.LogError("Не удалось найти все необходимые компоненты!");
            return;
        }

        // Инициализируем UI
        uiManager.Initialize();
        
        // Запускаем битву
        StartCoroutine(StartBattle());
        
        Debug.Log("=== ЗАВЕРШЕНИЕ ИНИЦИАЛИЗАЦИИ ИГРЫ ===");
    }

    private IEnumerator StartBattle()
    {
        Debug.Log("=== НАЧАЛО БИТВЫ ===");
        
        yield return new WaitForSeconds(battleStartDelay);
        
        isBattleActive = true;
        currentTurn = 1;
        
        // Показываем UI элементы
        uiManager.ShowActionButtons(true);
        uiManager.EnablePlayerSelection(true);
        
        // Обновляем информацию о ходе
        uiManager.UpdateTurnCounter(currentTurn, maxTurns);
        uiManager.ShowMessage("Битва началась!");
        
        // Генерируем начальные карты
        cardManager.StartNewTurn();
        
        Debug.Log($"Битва началась. Ход: {currentTurn}/{maxTurns}");
    }

    public void StartNewTurn()
    {
        if (currentTurn > maxTurns)
        {
            EndBattle();
            return;
        }

        cardManager.StartNewTurn();
        battleSystem.StartNewTurn();
        
        uiManager.UpdateTurnCounter(currentTurn, maxTurns);
        uiManager.ShowMessage($"Ход {currentTurn}");
    }

    public void EndTurn()
    {
        currentTurn++;
        
        if (currentTurn > maxTurns)
        {
            EndBattle();
        }
        else
        {
            StartNewTurn();
        }
    }

    private void EndBattle()
    {
        isBattleActive = false;
        BattleResult result = battleSystem.DetermineBattleResult();
        
        string resultMessage = "Ничья!";
        
        if (result == BattleResult.PlayerWin)
        {
            resultMessage = "Победа игрока!";
        }
        else if (result == BattleResult.EnemyWin)
        {
            resultMessage = "Победа противника!";
        }
        
        // Получаем статистику боя из BattleSystem
        BattleStatistics stats = battleSystem.GetBattleStatistics();
        uiManager.ShowBattleResult(resultMessage, stats.TotalTurns, stats.DamageDealt, stats.DamageReceived);
    }

    public void RestartGame()
    {
        Debug.Log("=== НАЧАЛО ПЕРЕЗАПУСКА ИГРЫ ===");
        
        // Сбрасываем все состояния перед перезагрузкой сцены
        isBattleActive = false;
        currentTurn = 0;

        // Отписываемся от всех событий и сбрасываем состояния UI
        if (uiManager != null)
        {
            uiManager.UnsubscribeFromAllEvents();
            uiManager.HideAllPanels();
        }

        // Очищаем карты и сбрасываем состояние
        if (cardManager != null)
        {
            cardManager.ClearAllCards();
            cardManager.ResetState();
        }

        // Сбрасываем состояние боевой системы и персонажей
        if (battleSystem != null)
        {
            // Сначала получаем ссылки на отряды
            Squad playerSquad = battleSystem.GetPlayerSquad();
            Squad enemySquad = battleSystem.GetEnemySquad();

            // Сбрасываем состояния персонажей
            if (playerSquad != null)
            {
                foreach (Character character in playerSquad.GetAliveMembers())
                {
                    if (character != null)
                    {
                        character.ResetState();
                    }
                }
            }

            if (enemySquad != null)
            {
                foreach (Character character in enemySquad.GetAliveMembers())
                {
                    if (character != null)
                    {
                        character.ResetState();
                    }
                }
            }

            // Сбрасываем состояние боевой системы
            battleSystem.ResetState();
        }

        Debug.Log("=== ПЕРЕЗАГРУЗКА СЦЕНЫ ===");
        
        // Перезагружаем сцену
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
        
        Debug.Log("=== ЗАВЕРШЕНИЕ ПЕРЕЗАПУСКА ИГРЫ ===");
    }

    public int GetCurrentTurn()
    {
        return currentTurn;
    }

    public int GetMaxTurns()
    {
        return maxTurns;
    }

    public bool IsBattleActive()
    {
        return isBattleActive;
    }
}

public enum BattleResult
{
    PlayerWin,
    EnemyWin,
    Draw
} 