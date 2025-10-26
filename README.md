# VoiceToUI
Enabling UI Navigation through voice commands

## HOW IT WORKS:

UI navigation system registeres UI elements that contain the VoiceNavigableElement, and creates a logical graph connecting all of these items hierarchicaly. This map is then fed in a HTTP request to OpenAI API, together with the voice command translated to text. The request to the AI also includes an exhaustive explanation of how to interpret the Node graph. 

To note, the hierarchy defined by the graph, doesn't have to match the actual Transform hierarchy of the gameobjects. This allows for flexibility decided more logically what is a "logical child" or "logical parent" without having to constraint the UI layout development in a prefab. To allow this, VoiceNavigableElement is a component that allows us to define the "Context" and the "Label". Setting a context override, acts as a "hierarchy redirect" allowing that disasociation from actual transform hierarchy.

## KEY CLASSES:
* `QueryProcessor` -Where the HTTP request is made to the AI
* `VoiceNavigationSystem` - main logic of the system. Handles the node graph and interacts with Voice & QueryProcessor
* `VoiceNavigableElement` - component added to gameobjects - this marks the object as recognizable by the VoiceNavigationSystem

###
Nodes are defined as such:
* `VoiceNavigationElement`
* `Node ParentNode`
* `VoiceNavigableType` (Button, Context, Menu). - Only Button is interactable. The intention of Context and Menu are to give the AI better context of how the hierarchy looks and understanding how everything is laid out

The output is the node which is the best match for the voice prompt. If that is valid, then the voice navigation system perfoms the actual UI navigation to the requested place. This includes a 2 second delay between each simulated button click so that we can visually see how the UI advances.

## HOW TO TEST IT:
* download the files and open the project with Unity engine.
* default scene is the demo, so just can just hit play
* Demo automatically records voice, so you can start sending prompts with voice as soon as you hit play. 
* Recommendable to first familiarize yourself with the UI screens so that you can make meaningful voice requests. Game demo consist of 4 scenes: Main menu, Inventory, Credits, Settings

## DEBUG:
UI shows a layer with the following information
* "Recording..." when the system detects voice from the microphone
* "Requesting" when the request has been sent to the AI, and we are waiting for a response
* "Processing" when the request has been received and we are performing the UI navigation acttions actions
* "Selected" - the selected node tag
* "Reason" - Short reasoning from the AI to why for why it chose the selected node.
