using UnityEngine.UIElements;
using System.Collections.Generic;
using System;
using System.Linq;

namespace UnityAIHelper.Editor.UI
{
    /// <summary>
    /// 支持重复值的下拉菜单，使用索引作为值
    /// </summary>
    public class IndexedDropdownField : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<IndexedDropdownField, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        private readonly PopupField<int> popupField;
        private readonly List<string> displayTexts = new List<string>();
        private int selectedIndex = -1;

        public event Action<int> OnSelectedIndexChanged;

        public IReadOnlyList<string> choices
        {
            get => displayTexts;
            set
            {
                displayTexts.Clear();
                
                // Create a dictionary to track occurrences of each text
                var textCounts = new Dictionary<string, int>();
                var uniqueTexts = new List<string>();
                
                foreach (var text in value)
                {
                    if (!textCounts.TryAdd(text, 1))
                    {
                        textCounts[text]++;
                        uniqueTexts.Add($"{text} ({textCounts[text]-1})");
                    }
                    else
                    {
                        uniqueTexts.Add(text);
                    }
                }
                
                displayTexts.AddRange(uniqueTexts);
                
                var indices = new List<int>();
                for (int i = 0; i < uniqueTexts.Count; i++)
                {
                    indices.Add(i);
                }
                
                popupField.choices = indices;
                if (indices.Count > 0)
                {
                    popupField.value = indices[0];
                    selectedIndex = indices[0];
                }
            }
        }

        public int index
        {
            get => selectedIndex;
            set
            {
                if (value >= -1 && value < displayTexts.Count)
                {
                    selectedIndex = value;
                    if (value >= 0)
                    {
                        popupField.value = value;
                    }
                }
            }
        }

        public string selectedText
        {
            get => selectedIndex >= 0 && selectedIndex < displayTexts.Count ? displayTexts[selectedIndex] : string.Empty;
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    var index = displayTexts.IndexOf(value);
                    if (index >= 0)
                    {
                        this.index = index;
                    }
                }
            }
        }

        public IndexedDropdownField()
        {
            popupField = new PopupField<int>();
            popupField.formatListItemCallback = (index) =>
            {
                if (index >= displayTexts.Count) return "";
                return displayTexts[index];
            };
            popupField.formatSelectedValueCallback = (index) =>
            {
                if (index >= displayTexts.Count) return "";
                return displayTexts[index];
            };
            
            popupField.RegisterValueChangedCallback(evt =>
            {
                selectedIndex = evt.newValue;
                OnSelectedIndexChanged?.Invoke(selectedIndex);
            });

            Add(popupField);
            style.flexGrow = 1;
        }
    }
}
