using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Code
{
    public class InventoryUI : Menu
    {
        [Serializable]
        struct InventorySlot
        {
            public Button ItemButton;
            public Texture ItemIcon;
        }
        
        [SerializeField]
        private InventorySlot[] _slots;
    
        [SerializeField]
        private RawImage _selectedItemImage;

        [SerializeField]
        private Transform _selectedItemLayer;
        
        // Update is called once per frame
        void Update()
        {
            var selectedGameObject = EventSystem.current.currentSelectedGameObject;
            foreach (var slot in _slots)
            {
                if (slot.ItemButton.gameObject == selectedGameObject)
                {
                    _selectedItemImage.texture = slot.ItemIcon;
                    _selectedItemLayer.gameObject.SetActive(true);
                    return;
                }
            }
            _selectedItemLayer.gameObject.SetActive(false);
        }

        void Start()
        {
            foreach (var slot in _slots)
            {
                void OnButtonClicked()
                {
                    EventSystem.current.SetSelectedGameObject(slot.ItemButton.gameObject);
                }
                slot.ItemButton.onClick.AddListener(OnButtonClicked);
            }
        }
    }
}