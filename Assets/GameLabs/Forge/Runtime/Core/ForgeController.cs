using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using GameLabs.Forge.Integration.OpenAI;

namespace GameLabs.Forge
{
    /// <summary>Click "Call Forge Now" in Inspector to test a round-trip.</summary>
    [ExecuteAlways]
    public class ForgeController : MonoBehaviour
    {
        [Header("Prompt Inputs")]
        [TextArea] public string GameDescription = "A traditional roguelike where the player is a rogue virus.";
        [Min(1)] public int ItemCount = 8;
        
        [Header("Save Options")]
        [Tooltip("Automatically save generated items as ScriptableObject assets (Editor only)")]
        public bool autoSaveAsAssets = true;
        
        [Tooltip("Custom folder name for saved assets (leave empty for 'GeneratedItem')")]
        public string customAssetFolder = "";
        
        [Header("Generated Items (Read-Only)")]
        [SerializeField] private List<GeneratedItem> lastGeneratedItems = new List<GeneratedItem>();
        
        /// <summary>Gets the last batch of generated items.</summary>
        public IReadOnlyList<GeneratedItem> LastGeneratedItems => lastGeneratedItems;
        
        /// <summary>
        /// Event fired when items are generated, allowing Editor code to hook in for auto-save.
        /// Parameters: (List of items, custom folder name)
        /// </summary>
        public event System.Action<List<GeneratedItem>, string> OnItemsGenerated;

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
                    GeneratedItemArray parsed = null;
                    try
                    {
                        parsed = JsonUtility.FromJson<GeneratedItemArray>(wrapped);
                    }
                    catch (Exception parseEx)
                    {
                        ForgeLogger.Error("Failed to parse generated items JSON: " + parseEx.Message);
                        return;
                    }
                    
                    if (parsed?.items == null || parsed.items.Length == 0)
                    {
                        ForgeLogger.Warn("No items parsed from payload.");
                        return;
                    }

                    ForgeLogger.Log($"Parsed {parsed.items.Length} items:");
                    
                    // Store and display items
                    lastGeneratedItems.Clear();
                    foreach (var item in parsed.items)
                    {
                        if (item == null) continue;
                        lastGeneratedItems.Add(item);
                        ForgeLogger.Log($"- {item.name ?? "Unnamed"} [{item.type ?? "Unknown"}] ({item.rarity ?? "None"})");
                    }
                    
                    // Notify listeners for auto-save (Editor code will handle this)
                    // This is outside the JSON parsing try-catch to separate concerns
                    if (autoSaveAsAssets && lastGeneratedItems.Count > 0)
                    {
                        try
                        {
                            OnItemsGenerated?.Invoke(lastGeneratedItems, customAssetFolder);
                        }
                        catch (Exception saveEx)
                        {
                            ForgeLogger.Error("Failed to auto-save items as assets: " + saveEx.Message + "\n" + saveEx.StackTrace);
                        }
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
        
        /// <summary>Clears the last generated items list.</summary>
        [ContextMenu("Forge/Clear Generated Items")]
        public void ClearGeneratedItems()
        {
            lastGeneratedItems.Clear();
            ForgeLogger.Log("Cleared generated items.");
        }
    }
}
