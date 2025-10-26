using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using static Code.QueryProcessor;

namespace Code
{
    public enum EVoiceNavigationState
    {
        Requesting,
        Processing,
        None
    }
    
    public enum ENavigableElementType
    {
        Context,
        Button,
        Menu
    }
    
    [Serializable]
    class NodeListWrapper
    {
        public List<NodeDefinition> Nodes;
    };
    
    public class VoiceNavigationSystem : MonoBehaviour
    {
        public static VoiceNavigationSystem Instance { get; private set; }
        private bool _printed = false;
        private QueryProcessor _queryProcessor;
        private List<NodeDefinition> _serializedNodes;
        
        
        private Coroutine _activeRequestCoroutine;
        private Coroutine _activeNavigationCoroutine;
        private Coroutine _recordingPulseCoroutine;
        private EVoiceNavigationState _currentState = EVoiceNavigationState.None;
        private bool _isRecording = false;
        private string _selectedNodeLabel = "";
        private string _selectedNodeReason = "";
        public EVoiceNavigationState CurrentState => _currentState;
        public bool IsRecording => _isRecording;
        public string SelectedNodeLabel => _selectedNodeLabel;
        public string SelectedNodeReason => _selectedNodeReason;
        
        class VnNode
        {
            public VoiceNavigableElement VoiceNavigableElement;
            public ENavigableElementType Type;
            public VnNode Parent;
            public string Tag; //only for debug purpose
        }

        private List<VnNode> _nodes = new List<VnNode>();

        void Awake()
        {
            // Ensure only one instance exists
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _queryProcessor = new QueryProcessor();
            DontDestroyOnLoad(gameObject);
        }
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            var children = GetComponentsInChildren<VoiceNavigableElement>(true);
            foreach (var child in children)
            {
                RegisterVoiceNavigableElement(child, child.GetElementType());
            }

            _queryProcessor.Start();
        }

        // Update is called once per frame
        void Update()
        {
            if (!_printed)
            {
                _printed = true;
                SerializeCurrentState();
                _queryProcessor.SetNodesMap(_serializedNodes);
            }
        }

        public void RegisterVoiceNavigableElement(VoiceNavigableElement voiceNavigableElement, ENavigableElementType elementType)
        {
            //insert children
            if (!TryFindNode(voiceNavigableElement, out VnNode node))
            {
                node = new VnNode{ VoiceNavigableElement = voiceNavigableElement, Type = elementType, Tag = voiceNavigableElement.GetTextId() };
                _nodes.Add(node);
            }

            var contextElement = node.VoiceNavigableElement.GetContextObjet();
            
            //update references to children and parent
            foreach (VnNode vnNode in _nodes)
            {
                if (vnNode == node)
                {
                    continue;
                }
                
                var currentNodeContext = vnNode.VoiceNavigableElement.GetContextObjet();

                /*if (currentNodeContext.transform.IsChildOf(contextElement.transform))
                {
                    if (!node.Children.Contains(vnNode))
                    {
                        node.Children.Add(vnNode);
                    }
                }
                else if (contextElement.transform.IsChildOf(currentNodeContext.transform))
                {
                    if (!vnNode.Children.Contains(node))
                    {
                        vnNode.Children.Add(node);
                    }
                }*/

                var parentComponent = GetFirstParentNavigable(voiceNavigableElement);
                if (parentComponent != null && TryFindNode(parentComponent, out VnNode parentNode))
                {
                    node.Parent = parentNode;
                }
            }
        }
        
       private VoiceNavigableElement GetFirstParentNavigable(VoiceNavigableElement voiceNavigableElement)
       {
           var contextObject = voiceNavigableElement.GetContextObjet();
           if (contextObject != voiceNavigableElement.gameObject)
           {
               return contextObject.GetComponent<VoiceNavigableElement>();
           }
           
            var parent = voiceNavigableElement.transform.parent;
            while (parent != null)
            {
                var parentElement = parent.GetComponent<VoiceNavigableElement>();
                if (parentElement != null)
                    return parentElement;

                parent = parent.parent;
            }

            return null; // reached top with no match
        }

        public void UnregisterVoiceNavigableElement(VoiceNavigableElement voiceNavigableElement)
        {
            if (TryFindNode(voiceNavigableElement, out VnNode foundNode))
            {
                //cleanup all references to the node we are deleting
                /*foreach (var node in _nodes)
                {
                    node.Children.Remove(foundNode);
                }*/
                _nodes.Remove(foundNode);
            }
            else
            {
                Debug.Log("VoiceNavigationSystem. Trying to unregistered node not found: " + voiceNavigableElement.name);
            }
            
        }
        
        bool TryFindNode(VoiceNavigableElement voiceNavigableElement, out VnNode foundNode)
        {
            foundNode = _nodes.Find( element => element.VoiceNavigableElement == voiceNavigableElement );
            return foundNode != null;
        }

        public void OnRecordingPulse()
        {
            // If a coroutine is already running, stop it (so timer resets)
            if (_recordingPulseCoroutine != null)
                StopCoroutine(_recordingPulseCoroutine);

            _isRecording = true;

            // Start a new coroutine that will reset the flag after 2 seconds
            _recordingPulseCoroutine = StartCoroutine(RecordingPulseTimer());

            _selectedNodeLabel = "";
            _selectedNodeReason = "";
        }

        private IEnumerator RecordingPulseTimer()
        {
            yield return new WaitForSeconds(2f);
            _isRecording = false;
            _recordingPulseCoroutine = null; // clear reference
        }
        
        public void RequestNavigation(string input)
        {
            Debug.Log("new query received: " + input);

            if (_activeRequestCoroutine != null)
            {
                StopCoroutine(_activeRequestCoroutine);
                
            }
            if (_activeNavigationCoroutine != null)
            {
                StopCoroutine(_activeNavigationCoroutine);
            }
            _activeRequestCoroutine = StartCoroutine(_queryProcessor.ProcessQuery(input, OnNavigationRequestCompleted));
            _currentState = EVoiceNavigationState.Requesting;
            _isRecording = false;
        }

        private void OnNavigationRequestCompleted(RequestResponse result)
        {
            if (result == null)
            {
                Debug.Log("VoiceNavigationSystem: Request failed");
                _currentState = EVoiceNavigationState.None;
            }
            else
            {
                Debug.Log("VoiceNavigationSystem: Request completed");
                ProcessResponse(result);
            }
        }

        private void ProcessResponse(RequestResponse result)
        {
            // Start the coroutine that will iterate through the path
            _activeNavigationCoroutine = StartCoroutine(ProcessResponseCoroutine(result));
            _currentState = EVoiceNavigationState.Processing;
        }

        private IEnumerator ProcessResponseCoroutine(RequestResponse result)
        {
            Debug.Log("VoiceNavigationResponse: " + result);
            
            
            if (result.SelectedId == null || result.SelectedId.Value < 0)
            {
                Debug.LogWarning("RouterResponse has empty or null PathToSelection.");
                _currentState = EVoiceNavigationState.None;
                _activeNavigationCoroutine = null;
                yield break;
            }

            if (result.SelectedId.Value > 0 && result.SelectedId.Value < _nodes.Count)
            {
                var node = _nodes[result.SelectedId.Value]; // int? (or -1 sentinel if using JsonUtility DTO)
                _selectedNodeLabel = node.Tag;
                _selectedNodeReason = result.Rationale;
            }

            var pathToSelection = BuildPathFromParents( result.SelectedId.Value );
            
            foreach (int nodeId in pathToSelection)
            {
                // Find the node with matching Id
                if (nodeId >= 0 && nodeId < _nodes.Count)
                {
                    var node = _nodes[nodeId];
                    if (node != null && node.Type == ENavigableElementType.Button && node.VoiceNavigableElement.TryGetComponent(out Button button))
                    {
                        Debug.Log($"Processing node {nodeId}: {node.VoiceNavigableElement?.GetTextId() ?? node.ToString()}");

                        button.onClick.Invoke();
                        // Wait 2 seconds before processing the next node
                        yield return new WaitForSeconds(2f);
                    }
                }
                else
                {
                    Debug.LogWarning($"Node with Id {nodeId} not found in _nodes list.");
                }
            }

            Debug.Log("Finished processing all nodes in path.");
            _currentState = EVoiceNavigationState.None;
            _activeNavigationCoroutine = null;
        }
        
        private List<int> BuildPathFromParents(int selectedId)
        {
            // Assuming nodes are indexed by Id (Id == index). If not, build a map first.
            var path = new List<int>();
            int current = selectedId;

            while (current >= 0 && current < _nodes.Count)
            {
                path.Add(current);
                var parentNode = _nodes[current].Parent; // int? (or -1 sentinel if using JsonUtility DTO)
                if (parentNode == null) break;    // reached root
                current = _nodes.IndexOf(parentNode);
            }

            path.Reverse(); // root -> ... -> selectedId
            return path;
        }
        
        private void SerializeCurrentState()
        {
            // Create a serializable type for nodes with unique Ids (index in the list)
            var nodeIdMap = _nodes
                .Select((node, idx) => new { node, idx })
                .ToDictionary(x => x.node, x => x.idx);

            // Add Parent index to each node for serialization
            _serializedNodes = _nodes
                .Select((node, idx) => new NodeDefinition{
                    Id = idx,
                    Type = node.Type.ToString(),
                    GameObjectName = node.VoiceNavigableElement != null ? node.VoiceNavigableElement.GetTextId() : "",
                    Parent = (node.Parent != null && nodeIdMap.ContainsKey(node.Parent)) ? nodeIdMap[node.Parent] : null,
                })
                .ToList();
            
            // Write the json string to Logs/ActiveMap.json, ensure directory exists
            string logsDir = System.IO.Path.Combine(UnityEngine.Application.dataPath, "..", "Logs");
            string filePath = System.IO.Path.Combine(logsDir, "ActiveMap.json");
            if (!System.IO.Directory.Exists(logsDir))
            {
                System.IO.Directory.CreateDirectory(logsDir);
            }

            var wrapper = new NodeListWrapper { Nodes = _serializedNodes };
            var jsonText = JsonConvert.SerializeObject(wrapper, Formatting.Indented);//JsonUtility.ToJson(wrapper, true);
            System.IO.File.WriteAllText(filePath, jsonText);
            Debug.Log("Serialized VoiceNavigationSystem State: " + jsonText);
        }
    }
}