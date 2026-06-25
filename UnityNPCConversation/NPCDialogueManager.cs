using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace LLM.NPCConversation
{
    public class NPCDialogueManager : MonoBehaviour
    {
        [Header("NPC Setup")]
        public NPCProfile npcProfile;

        [Header("LLM Settings")]
        public string apiUrl = "https://api.openai.com/v1/chat/completions";
        public string apiKey;
        public string model = "gpt-4o-mini";
        public int maxTokens = 250;

        public void StartConversation(string playerInput)
        {
            if (npcProfile == null)
            {
                Debug.LogWarning("NPCDialogueManager requires an NPCProfile to build conversation prompts.");
                return;
            }

            var prompt = BuildPrompt(playerInput);
            StartCoroutine(SendLLMRequestPrompt(prompt));
        }

        public string BuildPrompt(string playerInput)
        {
            var builder = new StringBuilder();
            builder.AppendLine("You are an NPC in a Unity game. Answer the player's question in character.");
            builder.AppendLine(npcProfile.BuildTraitSummary());
            builder.AppendLine("Conversation rules:");
            builder.AppendLine("- Stay in character based on the NPC traits and background.");
            builder.AppendLine("- Reflect the NPC's tone and current goal.");
            builder.AppendLine("- Keep responses concise enough for dialogue UI.");
            builder.AppendLine();
            builder.AppendLine($"Player: {playerInput}");
            builder.AppendLine("NPC:");
            return builder.ToString();
        }

        IEnumerator SendLLMRequestPrompt(string prompt)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Debug.LogError("LLM API key is not set in NPCDialogueManager.");
                yield break;
            }

            var requestData = new LLMRequestData
            {
                model = model,
                messages = new List<LLMMessage>
                {
                    new LLMMessage { role = "system", content = "You are a game NPC. Keep answers in character and friendly." },
                    new LLMMessage { role = "user", content = prompt }
                },
                max_tokens = maxTokens,
                temperature = 0.75f
            };

            var body = JsonUtility.ToJson(requestData);
            using (var request = new UnityWebRequest(apiUrl, "POST"))
            {
                var bodyRaw = Encoding.UTF8.GetBytes(body);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

                yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
                var hasError = request.result != UnityWebRequest.Result.Success;
#else
                var hasError = request.isNetworkError || request.isHttpError;
#endif
                if (hasError)
                {
                    Debug.LogError($"LLM request failed: {request.error}");
                    yield break;
                }

                var response = JsonUtility.FromJson<LLMResponse>(request.downloadHandler.text);
                if (response != null && response.choices != null && response.choices.Length > 0)
                {
                    var npcResponse = response.choices[0].message.content.Trim();
                    Debug.Log($"NPC Response: {npcResponse}");
                    OnNPCResponse(npcResponse);
                }
                else
                {
                    Debug.LogError("LLM response could not be parsed or contained no choices.");
                }
            }
        }

        protected virtual void OnNPCResponse(string response)
        {
            Debug.Log($"NPC says: {response}");
        }

        [System.Serializable]
        public class LLMRequestData
        {
            public string model;
            public List<LLMMessage> messages;
            public int max_tokens;
            public float temperature;
        }

        [System.Serializable]
        public class LLMMessage
        {
            public string role;
            public string content;
        }

        [System.Serializable]
        public class LLMResponse
        {
            public LLMChoice[] choices;
        }

        [System.Serializable]
        public class LLMChoice
        {
            public LLMMessage message;
        }
    }
}
