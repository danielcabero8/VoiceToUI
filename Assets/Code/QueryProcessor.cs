using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using System;

namespace Code
{
    public class QueryProcessor
    {
        [Serializable]
        public class RequestResponse
        {
            [JsonProperty("Action")]
            public string Action; // "navigate" | "ask_clarify"

            [JsonProperty("SelectedId")]
            public int? SelectedId;

            [JsonProperty("Confidence")]
            public float Confidence;

            [JsonProperty("Rationale")]
            public string Rationale;

            [JsonProperty("PathToSelection")]
            public List<int> PathToSelection;
        }
        
        [System.Serializable]
        public class ChatRequest {
            public string model;
            public List<RequestMessage> messages;
            public float temperature;
            public ResponseFormat response_format; 
        }
        
        public class ResponseFormat
        {
            public string type; // "text", "json_object", or "json_schema"
        }
        
        public class RequestMessage 
        { 
            public string role; 
            public string content; 
        }
        
        public class ChatChoiceMessage
        {
            public string role;
            public string content;
        }

        public class ChatChoice
        {
            public ChatChoiceMessage message;
        }

        public class ChatResponse
        {
            public ChatChoice[] choices;
        }
        
        [Serializable]
        public class NodeDefinition
        {
            public int Id;
            public string GameObjectName;
            public string Type;   // e.g. "menu", "button", "context"
            public int? Parent;   // null for root
        }

        [System.Serializable]
        public class PayloadDefinition
        {
            public string PlayerUtterance;   // The recognized speech as text
            public List<NodeDefinition> Nodes;    // Full static list of nodes
        }
        
        //SET THIS KEY FOR TESTING
        private string _key = "none";

        private string _initialPrompt = "You are a Voice UI Router for a Unity game.\n\nYour task is to select the single best UI node to navigate to, given:\n- A list of registered navigable nodes (each with Id, GameObjectName, Type, and Parent).\n- The player's voice command (natural language).\n\nDefinitions:\n- \"Button\": An actionable element (the player can click, press, or activate this). \n             These are always the final navigation targets.\n- \"Context\": A logical container that groups related elements (e.g., Weapons, Potions, Settings).\n             Contexts help define hierarchy but are not directly navigable.\n- \"Menu\": A special kind of Context that represents a primary or top-level navigation area\n          (e.g., Main Menu, Inventory). Menus also are not directly navigable.\n\nRules:\n1. Output STRICT JSON that matches the schema below exactly — no extra keys, arrays, or commentary.\n2. Use semantic understanding and common synonyms (e.g., “change my gun” \u2192 Weapons, “smokes” \u2192 Smoke Grenades).\n3. Only select \"Button\" nodes as the final target (SelectedId). \n   Contexts and Menus may appear in PathToSelection but must never be the final node.\n4. Prefer actionable elements first, then use Contexts and Menus to infer hierarchy.\n5. Never invent nodes. Only use Ids present in the provided Nodes list.\n6. If multiple candidates seem equally relevant, pick the one whose name is most specific or most directly referenced by the player's command.\n7. If the intent is unclear or no suitable Button fits, return Action = \"ask_clarify\" and provide a short clarification in Rationale explaining what needs to be clarified.\n8. Keep Rationale short (one or two brief sentences).\n9. Path completeness: PathToSelection must follow Parent pointers with no gaps. For each adjacent pair Path[i] -> Path[i+1], it must be true that Nodes[Path[i+1]].Parent == Path[i]. Example for “Laser Gun” (Id 8): [0, 1, 2, 5, 8].\n\nOutput ONLY a JSON object with this exact schema:\n\n{\n  \"Action\": \"navigate\" | \"ask_clarify\",\n  \"SelectedId\": integer | null,\n  \"Confidence\": number (0.0–1.0),\n  \"Rationale\": string,\n  \"PathToSelection\": [ integer ]\n}\n\n- The PathToSelection array must list node Ids in hierarchical order from the root to SelectedId, inclusive.\n- The last element in PathToSelection (SelectedId) must always correspond to a node of Type = \"Button\".\n- Do NOT include any text, commentary, or markdown outside the JSON object.";
        private List<RequestMessage> _requestMessages = new List<RequestMessage>();
        private List<NodeDefinition> _serializedNodes;

        public void Start()
        {
            // Initialize with system prompt
            _requestMessages.Add(new RequestMessage {
                role = "system",
                content = _initialPrompt
            });
        }
        
        public void SetNodesMap(List<NodeDefinition> nodesMap)
        {
            _serializedNodes = nodesMap;
        }
        
        public IEnumerator ProcessQuery(string input, System.Action<RequestResponse> onDone)
        {
            PayloadDefinition payloadDefinition = new PayloadDefinition
            {
                PlayerUtterance = input,
                Nodes = _serializedNodes,
            };
            
            // 2) Build messages for THIS request
            var messages = new List<RequestMessage>();

            // System rules only once per request (or reuse your cached string)
            messages.Add(new RequestMessage {
                role = "system",
                content = _initialPrompt // your routing rules + output schema
            });

            // User payload as JSON
            messages.Add(new RequestMessage {
                role = "user",
                content = JsonUtility.ToJson(payloadDefinition)
            });

            // 3) Request body
            var reqBody = new ChatRequest
            {
                model = "gpt-4o",
                messages = messages,
                temperature = 0.2f,
                response_format = new ResponseFormat { type = "json_object" },
                // max_tokens = 256,  // optional: cap output size
            };

            var json = JsonConvert.SerializeObject(reqBody);
            var req = new UnityWebRequest("https://api.openai.com/v1/chat/completions", "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            req.uploadHandler   = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", "Bearer " + _key);   // <- don’t hardcode in prod!

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success) {
                Debug.LogError($"OpenAI error: {req.responseCode} {req.error}\n{req.downloadHandler.text}");
                onDone?.Invoke(null);
                yield break;
            }

            // parse { choices[0].message.content }
            var resp = JsonConvert.DeserializeObject<ChatResponse>(req.downloadHandler.text);
            string text = resp.choices[0].message.content;
            
            var routerResponse = JsonConvert.DeserializeObject<RequestResponse>(text);

            Debug.Log($"Action: {routerResponse.Action}");
            Debug.Log($"SelectedId: {routerResponse.SelectedId}");
            Debug.Log($"Confidence: {routerResponse.Confidence}");
            Debug.Log($"Rationale: {routerResponse.Rationale}");
            Debug.Log($"Path: {string.Join(" -> ", routerResponse.PathToSelection)}");
            
            onDone?.Invoke(routerResponse);
        }
    }
}
