using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace GameLabs.Forge.Integration.OpenAI
{
    /// <summary>Editor-safe OpenAI chat client for FORGE (no external deps).</summary>
    [ExecuteAlways]
    public class ForgeOpenAIClient : MonoBehaviour
    {
        private static ForgeOpenAIClient _instance;
        public static ForgeOpenAIClient Instance
        {
            get
            {
                if (_instance != null) return _instance;

#if UNITY_EDITOR
                var go = GameObject.Find("~ForgeOpenAIClient") ?? new GameObject("~ForgeOpenAIClient");
                go.hideFlags = HideFlags.HideAndDontSave;
                _instance = go.GetComponent<ForgeOpenAIClient>() ?? go.AddComponent<ForgeOpenAIClient>();
#else
                var go = new GameObject("ForgeOpenAIClient");
                _instance = go.AddComponent<ForgeOpenAIClient>();
                DontDestroyOnLoad(go);
#endif
                return _instance;
            }
        }

        [Header("OpenAI")]
        [SerializeField] string apiUrl = "https://api.openai.com/v1/chat/completions";
        [SerializeField] string model = "gpt-4o-mini"; // cheap+fast default
        [Range(0f, 2f)][SerializeField] float temperature = 0.7f;

        string apiKey;
        string systemRole = "You are a helpful API.";
        string behavior;


        [Serializable]
        public class OpenAIResponse
        {
            public string id;
            public string @object;
            public int created;
            public string model;
            public List<Choice> choices;
            public Usage usage;
            public string system_fingerprint;
        }

        [Serializable]
        public class Choice
        {
            public int index;
            public Message message;
            public object logprobs;
            public string finish_reason;
        }

        [Serializable]
        public class Message
        {
            public string role;
            public string content;
        }

        [Serializable]
        public class Usage
        {
            public int prompt_tokens;
            public int completion_tokens;
            public int total_tokens;
        }

        [Serializable]
        public class RequestData
        {
            public string model;
            public List<Message> messages;
            public float temperature;
        }

        void OnEnable()
        {
            apiKey = ForgeConfig.GetOpenAIKey() ?? apiKey;
            if (string.IsNullOrEmpty(apiKey))
                ForgeLogger.Warn("Missing OpenAI API key. Use Setup Wizard or place forge.config.json.");
        }

        public void SetSystemRole(string role) => systemRole = role;
        public void SetBehavior(string desc) => behavior = desc;
        public void SetModel(string modelName) => model = modelName;
        public void SetTemperature(float temp) => temperature = Mathf.Clamp(temp, 0f, 2f);

        public void Chat(string userPrompt, Action<OpenAIResponse> cb)
        {
            AssertAPIKey();
            if (string.IsNullOrEmpty(apiKey)) { cb?.Invoke(null); return; }

            var msgs = new List<Message>
            {
                new Message{ role="system", content=systemRole },
                new Message{ role="user",   content= string.IsNullOrEmpty(behavior) ? userPrompt : behavior + "\n\n" + userPrompt }
            };

            var req = new RequestData { model = model, messages = msgs, temperature = temperature };
            var json = JsonUtility.ToJson(req);

            StartCoroutine(Post(apiUrl, json, cb));
        }

        IEnumerator Post(string url, string json, Action<OpenAIResponse> cb)
        {
            using var uwr = new UnityWebRequest(url, "POST");
            var body = Encoding.UTF8.GetBytes(json);
            uwr.uploadHandler = new UploadHandlerRaw(body);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");
            uwr.SetRequestHeader("Authorization", "Bearer " + apiKey);

            ForgeLogger.Log("Sending OpenAI chat request…");
            var op = uwr.SendWebRequest();
            yield return op;

            if (uwr.result == UnityWebRequest.Result.ConnectionError ||
                uwr.result == UnityWebRequest.Result.ProtocolError)
            {
                ForgeLogger.ErrorFull("HTTP error", uwr.downloadHandler.text);
                cb?.Invoke(null);
                yield break;
            }

            if (uwr.responseCode != 200)
            {
                ForgeLogger.ErrorFull("Non-200 from OpenAI: " + uwr.responseCode, uwr.downloadHandler.text);
                cb?.Invoke(null);
                yield break;
            }

            OpenAIResponse parsed = null;
            try { parsed = JsonUtility.FromJson<OpenAIResponse>(uwr.downloadHandler.text); }
            catch (Exception e) { ForgeLogger.Error("JSON parse error: " + e.Message); }

            cb?.Invoke(parsed);
        }

        void AssertAPIKey()
        {
            if (!string.IsNullOrEmpty(apiKey)) return;
            apiKey = ForgeConfig.GetOpenAIKey();
            if (string.IsNullOrEmpty(apiKey))
                ForgeLogger.Error("API key missing. Create Settings/forge.config.json or use Setup Wizard.");
        }
    }
}
