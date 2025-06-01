using UnityEngine;
using UnityEngine.EventSystems;

public class GridCell : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;
    private Vector2Int gridPosition;
    public bool isWalkable = true;
    public bool isHighlighted = false;

    private void Awake()
    {
        SetupSpriteRenderer();
        SetupCollider();
    }

    private void SetupSpriteRenderer()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        
        // Создаем полупрозрачный белый квадрат если нет спрайта
        if (spriteRenderer.sprite == null)
        {
            CreateDefaultSprite();
        }

        // Устанавливаем сортировку, чтобы клетки были под персонажами
        spriteRenderer.sortingOrder = -1;
    }

    private void CreateDefaultSprite()
    {
        Texture2D texture = new Texture2D(32, 32);
        Color[] colors = new Color[32 * 32];
        Color cellColor = new Color(1f, 1f, 1f, 0.2f); // Полупрозрачный белый цвет
        
        // Создаем рамку для лучшей видимости границ клетки
        for (int x = 0; x < 32; x++)
        {
            for (int y = 0; y < 32; y++)
            {
                if (x == 0 || x == 31 || y == 0 || y == 31)
                {
                    colors[y * 32 + x] = new Color(1f, 1f, 1f, 0.4f); // Более яркая рамка
                }
                else
                {
                    colors[y * 32 + x] = cellColor;
                }
            }
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        
        Sprite sprite = Sprite.Create(texture, 
            new Rect(0, 0, 32, 32), 
            new Vector2(0.5f, 0.5f), // Pivot в центре
            32);
        spriteRenderer.sprite = sprite;
    }

    private void SetupCollider()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider2D>();
        }

        // Настраиваем коллайдер точно по размеру спрайта
        if (spriteRenderer.sprite != null)
        {
            boxCollider.size = spriteRenderer.sprite.bounds.size;
        }
        else
        {
            boxCollider.size = new Vector2(1f, 1f);
        }

        // Убеждаемся, что коллайдер центрирован
        boxCollider.offset = Vector2.zero;
        
        // Для отладки выводим информацию о размерах и позиции
        Debug.Log($"GridCell {gridPosition}: Collider size = {boxCollider.size}, offset = {boxCollider.offset}");
    }

    public Vector2Int GetGridPosition()
    {
        return gridPosition;
    }

    public void SetGridPosition(Vector2Int pos)
    {
        gridPosition = pos;
        name = $"GridCell_{pos.x}_{pos.y}"; // Обновляем имя для удобства отладки
    }

    public void SetColor(Color color)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
    }

    private void OnMouseDown()
    {
        Debug.Log($"GridCell: Клик по клетке {gridPosition}");
        GridManager.Instance.OnCellClicked(this);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Отрисовываем границы коллайдера для отладки
        if (boxCollider != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 center = transform.position + (Vector3)boxCollider.offset;
            Vector3 size = boxCollider.size;
            Gizmos.DrawWireCube(center, size);
        }
    }
#endif
} 