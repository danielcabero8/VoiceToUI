using UnityEngine;

namespace Code
{
    public class MainMenu : Menu
    {
        [SerializeField] private Transform _inventoryMenu;
        [SerializeField] private Transform _creditsMenu;
        [SerializeField] private Transform _settingsMenu;
        
        [SerializeField] private Transform _mainMenuLayer;
        
        // Update is called once per frame
        void Update()
        {
            bool isSubmenuActive =
                (_inventoryMenu != null && _inventoryMenu.gameObject.activeInHierarchy)
                || (_creditsMenu != null && _creditsMenu.gameObject.activeInHierarchy)
                || (_settingsMenu != null && _settingsMenu.gameObject.activeInHierarchy);
            
            _mainMenuLayer.gameObject.SetActive(!isSubmenuActive);
        }
    }
}

