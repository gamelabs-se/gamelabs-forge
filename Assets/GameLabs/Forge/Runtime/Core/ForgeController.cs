using System;
using UnityEngine;
using GameLabs.Forge.Integration.OpenAI;

namespace GameLabs.Forge
{
    /// <summary>Click “Call Forge Now” in Inspector to test a round-trip.</summary>
    [ExecuteAlways]
    public class ForgeController : MonoBehaviour
    {
        [Header("Prompt Inputs")]
        [TextArea] public string GameDescription = "A traditional roguelike where the player is a rogue virus.";
        [Min(1)] public int ItemCount = 8;

        [ContextMenu("Forge/Call Forge")]
        public void CallForge()
        {
            var client = ForgeOpenAIClient.Instance;
            client.SetSystemRole("You are a game item API.");
            client.SetBehavior(@"
            You MUST respond with a JSON that can be serialized to:
            public class ResponseData { public int statusCode; public string data; }
            On error, return statusCode=500 and put the error message in 'data'.
            ");

            var itemTemplate = @"
            {
            ""name"": ""string"",
            ""type"": ""string"",
            ""rarity"": ""string"",
            ""attributes"": { ""attack"": 0, ""defense"": 0, ""healing"": 0 },
            ""description"": ""string""
            }";

            var prompt =
                $@"You are a game item generator. Generate {ItemCount} items in a JSON array matching the itemTemplate exactly.
                For non-applicable numeric fields, use 0. Keep strings short and game-ready.

                gameDescription: {GameDescription}
                itemTemplate: {itemTemplate}

                Return ONLY:
                {{""statusCode"":200,""data"":""<the JSON array string, escaped>""}}";

            client.Chat(prompt, HandleResponse);
        }

        [Serializable]
        public class ResponseData { public int statusCode; public string data; }

        void HandleResponse(ForgeOpenAIClient.OpenAIResponse r)
        {
            if (r == null) { ForgeLogger.Error("API call failed."); return; }

            var content = r.choices != null && r.choices.Count > 0 ? r.choices[0].message.content : null;
            if (string.IsNullOrEmpty(content)) { ForgeLogger.Error("Empty content from model."); return; }

            ForgeLogger.Log("OpenAI message:\n" + content);

            // Optional: parse ResponseData
            try
            {
                var dto = JsonUtility.FromJson<ResponseData>(content);
                if (dto?.statusCode == 200)
                {
                    ForgeLogger.Log("Items payload (escaped JSON):\n" + dto.data);
                    // TODO: Unescape and parse dto.data into items (JsonUtility or Newtonsoft).
                }
                else
                {
                    ForgeLogger.Error("Generator returned error: " + (dto?.data ?? "unknown"));
                }
            }
            catch (Exception e)
            {
                ForgeLogger.Error("Failed to parse ResponseData: " + e.Message);
            }
        }
    }
}
