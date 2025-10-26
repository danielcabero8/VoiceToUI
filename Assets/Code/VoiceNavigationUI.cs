using System;
using TMPro;
using UnityEngine;

namespace Code
{
    public class VoiceNavigationUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _currentStateText;
        [SerializeField] private TMP_Text _selectedNodeText;
        [SerializeField] private TMP_Text _selectedNodeTextReason;
        [SerializeField] private Transform _recordingLayer;
        [SerializeField] private Transform _resultLayer;
        
        // Update is called once per frame
        void Update()
        {
            var stateText = VoiceNavigationSystem.Instance.CurrentState switch
            {
                EVoiceNavigationState.Requesting => "REQUESTING",
                EVoiceNavigationState.Processing => "PROCESSING",
                EVoiceNavigationState.None => "",
                _ => throw new ArgumentOutOfRangeException()
            };
            _currentStateText.text = stateText;
            _recordingLayer.gameObject.SetActive(VoiceNavigationSystem.Instance.IsRecording);

            _selectedNodeText.text = VoiceNavigationSystem.Instance.SelectedNodeLabel;
            _selectedNodeTextReason.text = VoiceNavigationSystem.Instance.SelectedNodeReason;
            bool reasonValid = _selectedNodeText.text.Length > 0 || _selectedNodeTextReason.text.Length > 0;
            _resultLayer.gameObject.SetActive(reasonValid);
        }
    }
}