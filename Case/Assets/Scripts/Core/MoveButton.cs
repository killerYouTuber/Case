using UnityEngine;
using UnityEngine.UI;

public class MoveButton : MonoBehaviour
{
    private Button button;
    private GridCell[] allCells;
    public GameObject player; // Перетащите объект игрока сюда в инспекторе

    void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnMoveButtonClick);
        allCells = FindObjectsByType<GridCell>(FindObjectsSortMode.None);
    }

    void OnMoveButtonClick()
    {
        // First clear highlights from all cells
        foreach (var cell in allCells)
        {
            cell.SetColor(GridManager.Instance.DefaultCellColor);
            cell.isHighlighted = false;
        }

        if (player != null)
        {
            // Find cells within 3 units of the player
            Vector3 playerPos = player.transform.position;
            foreach (var cell in allCells)
            {
                Vector2Int cellGridPos = cell.GetGridPosition();
                Character character = player.GetComponent<Character>();
                if (character != null)
                {
                    Vector2Int playerGridPos = character.GetPosition();
                    int distance = Mathf.Abs(cellGridPos.x - playerGridPos.x) + Mathf.Abs(cellGridPos.y - playerGridPos.y);
                    
                    // Highlight cells that are in a straight line from the player and within 3 cells
                    if (distance <= 3 && 
                        (cellGridPos.x == playerGridPos.x || cellGridPos.y == playerGridPos.y))
                    {
                        cell.SetColor(GridManager.Instance.MoveRangeColor);
                        cell.isHighlighted = true;
                    }
                }
            }
        }
    }
} 