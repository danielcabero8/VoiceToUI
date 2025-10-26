using Code;
using UnityEngine;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{
    [SerializeField]
    Button _exitButton;

    void Awake()
    {
        if (_exitButton != null)
        {
            void ExitMenu()
            {
                //VoiceNavigationSystem.Instance.RequestNavigation("I want to change the gun I have equipped");
                gameObject.SetActive(false);
            }
            _exitButton.onClick.AddListener(ExitMenu);
        }
    }
}
