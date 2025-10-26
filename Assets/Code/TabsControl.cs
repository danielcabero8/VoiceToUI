using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TabsControl : MonoBehaviour
{
    [Serializable]
    struct TabEntry
    {
        public GameObject Button;
        public Transform TabContent;
    }

    [SerializeField]
    private TabEntry[] tabs;
    
    [SerializeField]
    private int startingTabIndex = 0;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            if (tabs[i].Button.TryGetComponent<Button>(out Button tabButton))
            {
                var indexToSelect = i;
                void OnSelect()
                {
                    SelectTab(indexToSelect);
                }

                tabButton.onClick.AddListener(OnSelect);
            }
        }
        
        SelectTab(startingTabIndex);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void SelectTab(int index)
    {
        int i = 0;
        foreach (var tabEntry in tabs)
        {
            if (tabEntry.TabContent != null)
            {
                tabEntry.TabContent.gameObject.SetActive( i == index );
                if (i == index)
                {
                    EventSystem.current.SetSelectedGameObject(tabEntry.Button );
                }
            }
            i++;    
        }
    }
}
