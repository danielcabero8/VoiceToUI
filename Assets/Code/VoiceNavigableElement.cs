using System;
using Code;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code
{
    public class VoiceNavigableElement : MonoBehaviour
    {
        [SerializeField] private Transform labelOverride;
        [SerializeField] private Transform contextOverride;
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            VoiceNavigationSystem.Instance.RegisterVoiceNavigableElement(this, GetElementType());
        }

        private void OnDestroy()
        {
            if (VoiceNavigationSystem.Instance != null)
            {
                VoiceNavigationSystem.Instance.UnregisterVoiceNavigableElement(this);
            }
        }

        public string GetTextId()
        {
            if (GetLabelObject().TryGetComponent(out TMP_Text tmpText) && tmpText.text.Length > 0)
            {
                return tmpText.text;
            }

            return gameObject.name;
        }

        public ENavigableElementType GetElementType()
        {
            ENavigableElementType elementType = ENavigableElementType.Context;
            if( gameObject.TryGetComponent(out Button button))
            {
                elementType = ENavigableElementType.Button;
            }
            if( gameObject.TryGetComponent(out Menu menu))
            {
                elementType = ENavigableElementType.Menu;
            }
            return elementType;
        }
        
        public GameObject GetContextObjet()
        {
            if (contextOverride != null)
            {
                return contextOverride.gameObject;
            }
            return gameObject;
        }
        
        private GameObject GetLabelObject()
        {
            if (labelOverride != null)
            {
                return labelOverride.gameObject;
            }
            return gameObject;
        }
    }
}
