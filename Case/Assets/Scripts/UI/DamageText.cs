using UnityEngine;
using TMPro;
using System.Collections;

public class DamageText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textComponent;
    [SerializeField] private float floatSpeed = 1f;
    [SerializeField] private float fadeSpeed = 1f;
    [SerializeField] private float lifeTime = 1.5f;
    [SerializeField] private Color defaultColor = Color.red;

    private Vector3 worldPosition;
    
    private void Awake()
    {
        if (textComponent == null)
        {
            textComponent = GetComponentInChildren<TextMeshProUGUI>();
        }
        
        if (textComponent != null)
        {
            textComponent.color = defaultColor;
        }
    }

    private void Start()
    {
        // Сохраняем начальную мировую позицию
        worldPosition = transform.position;
        StartCoroutine(FloatAndFade());
    }

    public void SetText(string text)
    {
        if (textComponent != null)
        {
            textComponent.text = text;
        }
    }
    
    public void SetColor(Color color)
    {
        if (textComponent != null)
        {
            textComponent.color = color;
            defaultColor = color;
        }
    }

    public void SetWorldPosition(Vector3 position)
    {
        worldPosition = position;
        if (transform != null)
        {
            transform.position = position;
        }
    }
    
    private IEnumerator FloatAndFade()
    {
        float elapsedTime = 0f;
        Color startColor = textComponent.color;
        Vector3 startPosition = transform.position;
        
        while (elapsedTime < lifeTime)
        {
            // Поднимаем текст вверх
            transform.position = startPosition + Vector3.up * floatSpeed * elapsedTime;
            
            // Затухание
            if (textComponent != null)
            {
                Color newColor = startColor;
                newColor.a = Mathf.Lerp(1f, 0f, elapsedTime / lifeTime * fadeSpeed);
                textComponent.color = newColor;
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        Destroy(gameObject);
    }
} 