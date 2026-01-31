#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameLabs.Forge.Editor
{
    /// <summary>
    /// Interactive review window for generated items.
    /// Allows accepting, discarding, or providing feedback on each item.
    /// </summary>
    public class ForgeReviewWindow : EditorWindow
    {
        // Items to review
        private List<ScriptableObject> _items = new();
        private int _currentIndex = 0;
        private Vector2 _scroll;
        private Vector2 _inspectorScroll;
        
        // Decision tracking
        private enum ItemDecision { Pending, Accepted, Discarded, DiscardedWithFeedback }
        private Dictionary<ScriptableObject, ItemDecision> _decisions = new();
        private Dictionary<ScriptableObject, string> _discardFeedback = new();
        
        // Accumulated feedback for next generation
        private List<string> _feedbackMessages = new();
        
        // Callbacks
        private Action<List<ScriptableObject>, List<string>> _onComplete;
        private Action _onGenerateMore;
        private Func<int, string, IEnumerator<object>> _generateMoreFunc;
        
        // State
        private bool _isGenerating = false;
        private string _currentFeedbackInput = "";
        private bool _showFeedbackInput = false;
        
        // Configuration from parent
        private int _generateCount = 10;
        private string _sessionInstructions = "";
        
        // Embedded inspector
        private UnityEditor.Editor _itemEditor;
        
        /// <summary>
        /// Opens the review window with the given items.
        /// </summary>
        public static ForgeReviewWindow Open(
            List<ScriptableObject> items,
            Action<List<ScriptableObject>, List<string>> onComplete,
            Action onGenerateMore = null,
            int generateCount = 10,
            string sessionInstructions = "")
        {
            var window = GetWindow<ForgeReviewWindow>(true, "Review Generated Items", true);
            window.minSize = new Vector2(600, 700);
            window.maxSize = new Vector2(900, 1000);
            
            window._items = new List<ScriptableObject>(items);
            window._currentIndex = 0;
            window._decisions.Clear();
            window._discardFeedback.Clear();
            window._feedbackMessages.Clear();
            window._onComplete = onComplete;
            window._onGenerateMore = onGenerateMore;
            window._generateCount = generateCount;
            window._sessionInstructions = sessionInstructions;
            
            // Initialize all items as pending
            foreach (var item in items)
            {
                window._decisions[item] = ItemDecision.Pending;
            }
            
            window.Show();
            return window;
        }
        
        /// <summary>
        /// Adds more items to the review queue (for "generate more" functionality).
        /// </summary>
        public void AddItems(List<ScriptableObject> newItems)
        {
            foreach (var item in newItems)
            {
                _items.Add(item);
                _decisions[item] = ItemDecision.Pending;
            }
            _isGenerating = false;
            Repaint();
        }
        
        /// <summary>
        /// Gets the accumulated feedback messages for the next generation.
        /// </summary>
        public List<string> GetAccumulatedFeedback() => new List<string>(_feedbackMessages);
        
        private void OnDestroy()
        {
            // Clean up editor
            if (_itemEditor != null)
            {
                DestroyImmediate(_itemEditor);
            }
            
            // If window closed without completing, treat pending items as discarded
            var accepted = _items.Where(i => _decisions.ContainsKey(i) && _decisions[i] == ItemDecision.Accepted).ToList();
            _onComplete?.Invoke(accepted, _feedbackMessages);
        }
        
        private void OnGUI()
        {
            if (_items.Count == 0)
            {
                EditorGUILayout.HelpBox("No items to review.", MessageType.Info);
                return;
            }
            
            DrawHeader();
            DrawProgressBar();
            
            EditorGUILayout.Space(8);
            
            // Main content area
            EditorGUILayout.BeginHorizontal();
            
            // Left side: Item list
            EditorGUILayout.BeginVertical(GUILayout.Width(180));
            DrawItemList();
            EditorGUILayout.EndVertical();
            
            // Separator
            var sepRect = EditorGUILayout.GetControlRect(GUILayout.Width(1));
            EditorGUI.DrawRect(sepRect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
            
            // Right side: Item inspector
            EditorGUILayout.BeginVertical();
            DrawItemInspector();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(8);
            
            // Action buttons at bottom
            DrawActionButtons();
            
            // Feedback input popup
            if (_showFeedbackInput)
            {
                DrawFeedbackPopup();
            }
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.Space(8);
            
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            
            EditorGUILayout.LabelField("Review Generated Items", headerStyle);
            
            var subtitleStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                fontSize = 11
            };
            
            int pending = _decisions.Count(d => d.Value == ItemDecision.Pending);
            int accepted = _decisions.Count(d => d.Value == ItemDecision.Accepted);
            int discarded = _decisions.Count(d => d.Value == ItemDecision.Discarded || d.Value == ItemDecision.DiscardedWithFeedback);
            
            EditorGUILayout.LabelField($"{pending} pending · {accepted} accepted · {discarded} discarded", subtitleStyle);
            
            EditorGUILayout.Space(4);
        }
        
        private void DrawProgressBar()
        {
            int total = _items.Count;
            int reviewed = _decisions.Count(d => d.Value != ItemDecision.Pending);
            float progress = total > 0 ? (float)reviewed / total : 0;
            
            var rect = EditorGUILayout.GetControlRect(GUILayout.Height(6));
            rect.x += 16;
            rect.width -= 32;
            
            // Background
            EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f, 0.5f));
            
            // Progress (green for accepted ratio)
            int accepted = _decisions.Count(d => d.Value == ItemDecision.Accepted);
            float acceptedRatio = total > 0 ? (float)accepted / total : 0;
            var acceptedRect = new Rect(rect.x, rect.y, rect.width * acceptedRatio, rect.height);
            EditorGUI.DrawRect(acceptedRect, new Color(0.2f, 0.8f, 0.3f, 0.8f));
            
            // Discarded (red)
            int discarded = _decisions.Count(d => d.Value == ItemDecision.Discarded || d.Value == ItemDecision.DiscardedWithFeedback);
            float discardedRatio = total > 0 ? (float)discarded / total : 0;
            var discardedRect = new Rect(rect.x + rect.width * acceptedRatio, rect.y, rect.width * discardedRatio, rect.height);
            EditorGUI.DrawRect(discardedRect, new Color(0.8f, 0.3f, 0.3f, 0.8f));
        }
        
        private void DrawItemList()
        {
            EditorGUILayout.LabelField("Items", EditorStyles.boldLabel);
            
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
            
            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                if (item == null) continue;
                
                var decision = _decisions.ContainsKey(item) ? _decisions[item] : ItemDecision.Pending;
                
                // Style based on decision
                var style = new GUIStyle(EditorStyles.miniButton)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fixedHeight = 24,
                    padding = new RectOffset(8, 8, 2, 2)
                };
                
                // Highlight current item
                if (i == _currentIndex)
                {
                    GUI.backgroundColor = new Color(0.3f, 0.5f, 0.9f, 0.5f);
                }
                else
                {
                    GUI.backgroundColor = decision switch
                    {
                        ItemDecision.Accepted => new Color(0.2f, 0.7f, 0.3f, 0.3f),
                        ItemDecision.Discarded => new Color(0.7f, 0.3f, 0.3f, 0.3f),
                        ItemDecision.DiscardedWithFeedback => new Color(0.7f, 0.5f, 0.2f, 0.3f),
                        _ => Color.white
                    };
                }
                
                // Icon based on decision
                string icon = decision switch
                {
                    ItemDecision.Accepted => "✓ ",
                    ItemDecision.Discarded => "✗ ",
                    ItemDecision.DiscardedWithFeedback => "✗💬 ",
                    _ => "○ "
                };
                
                if (GUILayout.Button(icon + item.name, style))
                {
                    _currentIndex = i;
                    RefreshItemEditor();
                }
                
                GUI.backgroundColor = Color.white;
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawItemInspector()
        {
            if (_currentIndex < 0 || _currentIndex >= _items.Count)
            {
                EditorGUILayout.HelpBox("Select an item to inspect.", MessageType.Info);
                return;
            }
            
            var currentItem = _items[_currentIndex];
            if (currentItem == null)
            {
                EditorGUILayout.HelpBox("Item is null.", MessageType.Warning);
                return;
            }
            
            // Item header
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(currentItem.name, EditorStyles.boldLabel);
            
            var decision = _decisions.ContainsKey(currentItem) ? _decisions[currentItem] : ItemDecision.Pending;
            string statusText = decision switch
            {
                ItemDecision.Accepted => "ACCEPTED",
                ItemDecision.Discarded => "DISCARDED",
                ItemDecision.DiscardedWithFeedback => "DISCARDED (with feedback)",
                _ => "PENDING"
            };
            
            var statusStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontStyle = FontStyle.Bold
            };
            statusStyle.normal.textColor = decision switch
            {
                ItemDecision.Accepted => new Color(0.2f, 0.8f, 0.3f),
                ItemDecision.Discarded => new Color(0.8f, 0.3f, 0.3f),
                ItemDecision.DiscardedWithFeedback => new Color(0.8f, 0.6f, 0.2f),
                _ => Color.gray
            };
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(statusText, statusStyle, GUILayout.Width(150));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(4);
            
            // Embedded inspector
            _inspectorScroll = EditorGUILayout.BeginScrollView(_inspectorScroll, GUILayout.ExpandHeight(true));
            
            // Create/update editor for current item
            if (_itemEditor == null || _itemEditor.target != currentItem)
            {
                RefreshItemEditor();
            }
            
            if (_itemEditor != null)
            {
                _itemEditor.OnInspectorGUI();
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.Space(8);
            
            // Per-item action buttons
            DrawItemActionButtons(currentItem);
        }
        
        private void RefreshItemEditor()
        {
            if (_itemEditor != null)
            {
                DestroyImmediate(_itemEditor);
                _itemEditor = null;
            }
            
            if (_currentIndex >= 0 && _currentIndex < _items.Count && _items[_currentIndex] != null)
            {
                _itemEditor = UnityEditor.Editor.CreateEditor(_items[_currentIndex]);
            }
        }
        
        private void DrawItemActionButtons(ScriptableObject item)
        {
            var decision = _decisions.ContainsKey(item) ? _decisions[item] : ItemDecision.Pending;
            
            EditorGUILayout.BeginHorizontal();
            
            // Accept button
            GUI.backgroundColor = decision == ItemDecision.Accepted 
                ? new Color(0.2f, 0.9f, 0.3f, 0.8f) 
                : new Color(0.2f, 0.7f, 0.3f, 0.5f);
            
            if (GUILayout.Button(decision == ItemDecision.Accepted ? "✓ Accepted" : "Accept", GUILayout.Height(32)))
            {
                _decisions[item] = ItemDecision.Accepted;
                _discardFeedback.Remove(item);
                MoveToNextPending();
            }
            
            GUI.backgroundColor = Color.white;
            
            GUILayout.Space(8);
            
            // Discard button
            GUI.backgroundColor = (decision == ItemDecision.Discarded || decision == ItemDecision.DiscardedWithFeedback)
                ? new Color(0.9f, 0.3f, 0.3f, 0.8f)
                : new Color(0.7f, 0.3f, 0.3f, 0.5f);
            
            if (GUILayout.Button(decision == ItemDecision.Discarded ? "✗ Discarded" : "Discard", GUILayout.Height(32)))
            {
                _decisions[item] = ItemDecision.Discarded;
                _discardFeedback.Remove(item);
                MoveToNextPending();
            }
            
            GUI.backgroundColor = Color.white;
            
            GUILayout.Space(8);
            
            // Discard with feedback button
            GUI.backgroundColor = decision == ItemDecision.DiscardedWithFeedback
                ? new Color(0.9f, 0.6f, 0.2f, 0.8f)
                : new Color(0.7f, 0.5f, 0.2f, 0.5f);
            
            if (GUILayout.Button("Discard + Feedback", GUILayout.Height(32)))
            {
                _showFeedbackInput = true;
                _currentFeedbackInput = _discardFeedback.ContainsKey(item) ? _discardFeedback[item] : "";
            }
            
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndHorizontal();
            
            // Show existing feedback if any
            if (decision == ItemDecision.DiscardedWithFeedback && _discardFeedback.ContainsKey(item))
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox($"Feedback: {_discardFeedback[item]}", MessageType.Info);
            }
        }
        
        private void DrawFeedbackPopup()
        {
            // Semi-transparent overlay
            var fullRect = new Rect(0, 0, position.width, position.height);
            EditorGUI.DrawRect(fullRect, new Color(0, 0, 0, 0.5f));
            
            // Popup box
            float popupWidth = 400;
            float popupHeight = 200;
            var popupRect = new Rect(
                (position.width - popupWidth) / 2,
                (position.height - popupHeight) / 2,
                popupWidth,
                popupHeight
            );
            
            GUI.Box(popupRect, GUIContent.none, "window");
            
            GUILayout.BeginArea(new Rect(popupRect.x + 16, popupRect.y + 16, popupRect.width - 32, popupRect.height - 32));
            
            EditorGUILayout.LabelField("Why are you discarding this item?", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("This feedback will be sent to the AI for future generations.", EditorStyles.miniLabel);
            
            EditorGUILayout.Space(8);
            
            _currentFeedbackInput = EditorGUILayout.TextArea(_currentFeedbackInput, GUILayout.Height(80));
            
            EditorGUILayout.Space(8);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Cancel", GUILayout.Height(28)))
            {
                _showFeedbackInput = false;
                _currentFeedbackInput = "";
            }
            
            GUILayout.Space(8);
            
            GUI.enabled = !string.IsNullOrWhiteSpace(_currentFeedbackInput);
            if (GUILayout.Button("Submit Feedback", GUILayout.Height(28)))
            {
                var currentItem = _items[_currentIndex];
                _decisions[currentItem] = ItemDecision.DiscardedWithFeedback;
                _discardFeedback[currentItem] = _currentFeedbackInput;
                
                // Add to accumulated feedback (avoid duplicates)
                if (!_feedbackMessages.Contains(_currentFeedbackInput))
                {
                    _feedbackMessages.Add(_currentFeedbackInput);
                }
                
                _showFeedbackInput = false;
                _currentFeedbackInput = "";
                MoveToNextPending();
            }
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
            
            GUILayout.EndArea();
        }
        
        private void DrawActionButtons()
        {
            var divRect = EditorGUILayout.GetControlRect(GUILayout.Height(1));
            EditorGUI.DrawRect(divRect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
            
            EditorGUILayout.Space(8);
            
            int pending = _decisions.Count(d => d.Value == ItemDecision.Pending);
            int accepted = _decisions.Count(d => d.Value == ItemDecision.Accepted);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16);
            
            // Quick actions
            if (pending > 0)
            {
                if (GUILayout.Button($"Accept All Remaining ({pending})", GUILayout.Height(28), GUILayout.Width(180)))
                {
                    foreach (var item in _items)
                    {
                        if (_decisions[item] == ItemDecision.Pending)
                        {
                            _decisions[item] = ItemDecision.Accepted;
                        }
                    }
                    Repaint();
                }
                
                GUILayout.Space(8);
                
                if (GUILayout.Button($"Discard All Remaining ({pending})", GUILayout.Height(28), GUILayout.Width(180)))
                {
                    foreach (var item in _items)
                    {
                        if (_decisions[item] == ItemDecision.Pending)
                        {
                            _decisions[item] = ItemDecision.Discarded;
                        }
                    }
                    Repaint();
                }
            }
            
            GUILayout.FlexibleSpace();
            
            // Generate more button
            if (_onGenerateMore != null)
            {
                GUI.backgroundColor = new Color(0.3f, 0.5f, 0.9f, 0.7f);
                string genLabel = _isGenerating ? "Generating..." : $"Generate More ({_generateCount})";
                GUI.enabled = !_isGenerating;
                
                if (GUILayout.Button(genLabel, GUILayout.Height(28), GUILayout.Width(150)))
                {
                    _isGenerating = true;
                    _onGenerateMore?.Invoke();
                }
                
                GUI.enabled = true;
                GUI.backgroundColor = Color.white;
                
                GUILayout.Space(8);
            }
            
            // Finish button
            GUI.backgroundColor = accepted > 0 
                ? new Color(0.2f, 0.8f, 0.3f, 0.8f) 
                : new Color(0.5f, 0.5f, 0.5f, 0.5f);
            
            string finishLabel = accepted > 0 ? $"Finish & Save ({accepted})" : "Finish (nothing to save)";
            
            if (GUILayout.Button(finishLabel, GUILayout.Height(28), GUILayout.Width(180)))
            {
                FinishReview();
            }
            
            GUI.backgroundColor = Color.white;
            
            GUILayout.Space(16);
            EditorGUILayout.EndHorizontal();
            
            // Feedback summary
            if (_feedbackMessages.Count > 0)
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.HelpBox(
                    $"{_feedbackMessages.Count} feedback message(s) will be sent with the next generation.",
                    MessageType.Info
                );
            }
            
            EditorGUILayout.Space(8);
        }
        
        private void MoveToNextPending()
        {
            // Find next pending item
            for (int i = _currentIndex + 1; i < _items.Count; i++)
            {
                if (_decisions[_items[i]] == ItemDecision.Pending)
                {
                    _currentIndex = i;
                    RefreshItemEditor();
                    Repaint();
                    return;
                }
            }
            
            // Wrap around from beginning
            for (int i = 0; i < _currentIndex; i++)
            {
                if (_decisions[_items[i]] == ItemDecision.Pending)
                {
                    _currentIndex = i;
                    RefreshItemEditor();
                    Repaint();
                    return;
                }
            }
            
            // No pending items left - stay on current
            Repaint();
        }
        
        private void FinishReview()
        {
            // Collect accepted items
            var accepted = _items.Where(i => _decisions.ContainsKey(i) && _decisions[i] == ItemDecision.Accepted).ToList();
            
            // Destroy discarded items
            foreach (var item in _items)
            {
                if (_decisions[item] != ItemDecision.Accepted && item != null)
                {
                    DestroyImmediate(item);
                }
            }
            
            // Clear callback to prevent OnDestroy from calling it again
            var callback = _onComplete;
            var feedback = new List<string>(_feedbackMessages);
            _onComplete = null;
            
            // Close window
            Close();
            
            // Invoke callback with results
            callback?.Invoke(accepted, feedback);
        }
    }
}
#endif
