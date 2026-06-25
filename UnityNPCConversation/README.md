# Unity NPC Conversation System

This folder contains a simple Unity C# system for building NPC dialogue prompts from personality traits and sending them to a compatible LLM API.

## Files

- `NPCTrait.cs` - Serializable trait class for NPC personality attributes.
- `NPCProfile.cs` - ScriptableObject storing NPC name, role, background, tone, goal, and traits.
- `NPCDialogueManager.cs` - MonoBehaviour that builds prompts and sends them to an LLM endpoint.
- `NPCConversationExample.cs` - Example MonoBehaviour to demonstrate starting a conversation.

## Setup

1. Create a new Unity project or add this folder to an existing Unity project.
2. In Unity, right-click in the Project window and choose `Create > LLM > NPC Profile`.
3. Configure the NPC profile with traits like `Friendly`, `Grumpy`, `Truthful`, etc.
4. Add `NPCDialogueManager` to a GameObject and assign the profile.
5. Configure `apiUrl`, `apiKey`, and `model` for your LLM provider.
6. Use `StartConversation(playerInput)` to generate NPC dialogue.

## How it works

- `NPCProfile.BuildTraitSummary()` creates a human-readable prompt summary.
- `NPCDialogueManager.BuildPrompt()` assembles the final prompt with player input.
- `NPCDialogueManager.SendLLMRequestPrompt()` posts the prompt to an LLM API and logs the result.

## Notes

- This example uses UnityWebRequest and `JsonUtility` for simplicity.
- If you use a different LLM API format, adjust the request payload and response parsing.
- Secure your API key; avoid embedding it directly in build artifacts.
