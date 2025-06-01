using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(HorizontalLayoutGroup))]
public class CharacterInfoPanel : MonoBehaviour
{
    [SerializeField] private RectTransform playerInfoPanel;
    [SerializeField] private RectTransform enemyInfoPanel;

    private void Awake()
    {
        SetupPanels();
    }

    private void SetupPanels()
    {
        // Get the Horizontal Layout Group component
        HorizontalLayoutGroup horizontalLayout = GetComponent<HorizontalLayoutGroup>();
        
        // Configure the Horizontal Layout Group
        horizontalLayout.childControlWidth = true;
        horizontalLayout.childForceExpandWidth = true;
        horizontalLayout.childControlHeight = true;
        horizontalLayout.childForceExpandHeight = true;
        horizontalLayout.spacing = 10f; // Spacing between panels
        
        // Setup player panel
        if (playerInfoPanel != null)
        {
            SetupChildPanel(playerInfoPanel);
        }
        
        // Setup enemy panel
        if (enemyInfoPanel != null)
        {
            SetupChildPanel(enemyInfoPanel);
        }
    }

    private void SetupChildPanel(RectTransform panel)
    {
        // Get or add Layout Element component
        LayoutElement layoutElement = panel.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = panel.gameObject.AddComponent<LayoutElement>();
        }
        
        // Configure Layout Element
        layoutElement.flexibleWidth = 1; // This makes the panel stretch
        layoutElement.minWidth = 100; // Minimum width to prevent too narrow panels
        layoutElement.flexibleHeight = 1; // This makes the panel stretch vertically
    }
} 