using System;
using System.Text.RegularExpressions;
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

        [Serializable]
        public class ItemAttributes { public int attack; public int defense; public int healing; }

        [Serializable]
        public class GeneratedItem
        {
            public string name;
            public string type;
            public string rarity;
            public ItemAttributes attributes;
            public string description;
        }

        [Serializable]
        class GeneratedItemArray { public GeneratedItem[] items; }

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
                    var unescaped = Regex.Unescape(dto.data ?? string.Empty).Trim();
                    if (unescaped.Length >= 2 && unescaped[0] == '"' && unescaped[unescaped.Length - 1] == '"')
                        unescaped = unescaped.Substring(1, unescaped.Length - 2);

                    if (string.IsNullOrWhiteSpace(unescaped))
                    {
                        ForgeLogger.Warn("Items payload empty after unescaping.");
                        return;
                    }

                    var wrapped = $"{{\"items\":{unescaped}}}";
                    try
                    {
                        var parsed = JsonUtility.FromJson<GeneratedItemArray>(wrapped);
                        if (parsed?.items == null || parsed.items.Length == 0)
                        {
                            ForgeLogger.Warn("No items parsed from payload.");
                            return;
                        }

                        ForgeLogger.Log($"Parsed {parsed.items.Length} items:");
                        foreach (var item in parsed.items)
                        {
                            if (item == null) continue;
                            ForgeLogger.Log($"- {item.name} [{item.type}] ({item.rarity})");
                        }
                    }
                    catch (Exception parseEx)
                    {
                        ForgeLogger.Error("Failed to parse generated items: " + parseEx.Message);
                    }
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
