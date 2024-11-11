using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;

namespace UnityAIHelper.Editor
{
    public class CodeDiffViewer
    {
        private enum DiffType
        {
            None,
            Added,
            Deleted,
            Modified,
            Placeholder
        }

        private class CodeLine
        {
            public int LineNumber { get; set; }
            public string Content { get; set; }
            public float Height { get; set; }
            public DiffType DiffType { get; set; }
            public bool IsPlaceholder { get; set; }
        }

        private List<CodeLine> oldCodeLines = new List<CodeLine>();
        private List<CodeLine> newCodeLines = new List<CodeLine>();
        private Vector2 scrollPosition;
        private GUIStyle lineNumberStyle;
        private GUIStyle codeStyle;
        private GUIStyle headerStyle;
        private EditorWindow parentWindow;
        private const float LINE_NUMBER_WIDTH = 50f;
        private const float HEADER_HEIGHT = 20f;
        private float lastCalculatedWidth = 0f;

        // 差异显示的颜色
        private static readonly Color AddedColor = new Color(0.2f, 0.8f, 0.2f, 0.2f);     // 绿色
        private static readonly Color DeletedColor = new Color(0.8f, 0.2f, 0.2f, 0.2f);   // 红色
        private static readonly Color ModifiedColor = new Color(0.2f, 0.2f, 0.8f, 0.2f);  // 蓝色
        private static readonly Color AlternateColor = new Color(0.3f, 0.3f, 0.3f, 0.1f); // 交替行背景色

        public CodeDiffViewer(EditorWindow parentWindow)
        {
            this.parentWindow = parentWindow;
            InitializeStyles();
        }

        private void InitializeStyles()
        {
            lineNumberStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleRight,
                normal = new GUIStyleState
                {
                    textColor = EditorGUIUtility.isProSkin ? new Color(0.6f, 0.6f, 0.6f) : new Color(0.4f, 0.4f, 0.4f)
                },
                padding = new RectOffset(4, 4, 2, 2),
                fontSize = 11
            };

            codeStyle = new GUIStyle(EditorStyles.label)
            {
                normal = new GUIStyleState
                {
                    textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black,
                    background = null
                },
                wordWrap = true,
                richText = true,
                padding = new RectOffset(4, 4, 2, 2),
                fontSize = 12,
                stretchWidth = true,
                stretchHeight = true
            };

            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12
            };
        }

        public void SetCodes(string oldCode, string newCode)
        {
            oldCodeLines = SplitIntoLines(oldCode ?? "");
            newCodeLines = SplitIntoLines(newCode ?? "");
            
            // 计算差异并添加对齐空行
            CalculateDiffWithAlignment();

            if (parentWindow != null)
            {
                parentWindow.Repaint();
            }
        }

        private void CalculateDiffWithAlignment()
        {
            var oldLines = oldCodeLines.Select(l => l.Content).ToList();
            var newLines = newCodeLines.Select(l => l.Content).ToList();
            
            // 计算LCS矩阵
            var lcs = ComputeLCSMatrix(oldLines, newLines);
            
            // 创建新的对齐后的列表
            var alignedOldLines = new List<CodeLine>();
            var alignedNewLines = new List<CodeLine>();
                        
            DiffType lastDiff = DiffType.None;
            
            void UpdateSameLine(ref int i,ref int j)
            {
                var oldLine = oldCodeLines[i - 1];
                var newLine = newCodeLines[j - 1];
                oldLine.DiffType = DiffType.None;
                newLine.DiffType = DiffType.None;
                lastDiff = DiffType.None;
                alignedOldLines.Insert(0, oldLine);
                alignedNewLines.Insert(0, newLine);
                i--;
                j--;
            }

            void UpdateAddedLine(ref int i, ref int j)
            {
                var newLine = newCodeLines[j - 1];
                newLine.DiffType = DiffType.Added;
                lastDiff = DiffType.Added;
                alignedNewLines.Insert(0, newLine);
                var placeholder = CreatePlaceholderLine();
                alignedOldLines.Insert(0, placeholder);
                j--;
            }
            
            void UpdateDeletedLine(ref int i, ref int j)
            {
                var oldLine = oldCodeLines[i - 1];
                oldLine.DiffType = DiffType.Deleted;
                lastDiff = DiffType.Deleted;
                alignedOldLines.Insert(0, oldLine);
                var placeholder = CreatePlaceholderLine();
                alignedNewLines.Insert(0, placeholder);
                i--;
            }
            
            
            
            int i = oldLines.Count;
            int j = newLines.Count;
            // 从后向前回溯，添加对齐空行
            while (i > 0 || j > 0)
            {
                if (i > 0 && j > 0 && oldLines[i - 1] == newLines[j - 1])
                {
                    if (string.IsNullOrEmpty(oldLines[i - 1]))
                    {
                        switch (lastDiff)
                        {
                            case DiffType.None:
                                UpdateSameLine(ref i, ref j);
                                break;
                            case DiffType.Added:
                                UpdateAddedLine(ref i, ref j);
                                break;
                            case DiffType.Deleted:
                                UpdateDeletedLine(ref i, ref j);
                                break;
                        }
                    }
                    else
                    {
                        //相同的行
                        UpdateSameLine(ref i, ref j);
                    }
                }
                else if (j > 0 && (i == 0 || lcs[i, j - 1] >= lcs[i - 1, j]))
                {
                    // 新增的行
                    UpdateAddedLine(ref i, ref j);
                }
                else if (i > 0 && (j == 0 || lcs[i, j - 1] < lcs[i - 1, j]))
                {
                    // 删除的行
                    UpdateDeletedLine(ref i, ref j);
                }
            }

            // 更新行号
            UpdateLineNumbers(alignedOldLines);
            UpdateLineNumbers(alignedNewLines);

            oldCodeLines = alignedOldLines;
            newCodeLines = alignedNewLines;
        }

        private void UpdateLineNumbers(List<CodeLine> lines)
        {
            int lineNumber = 1;
            foreach (var line in lines)
            {
                if (!line.IsPlaceholder)
                {
                    line.LineNumber = lineNumber++;
                }
            }
        }

        private CodeLine CreatePlaceholderLine()
        {
            return new CodeLine
            {
                LineNumber = 0, // 空行不显示行号
                Content = "",
                Height = 0,
                DiffType = DiffType.Placeholder,
                IsPlaceholder = true,
            };
        }

        private int[,] ComputeLCSMatrix(List<string> oldLines, List<string> newLines)
        {
            int[,] lcs = new int[oldLines.Count + 1, newLines.Count + 1];

            for (int i = 1; i <= oldLines.Count; i++)
            {
                for (int j = 1; j <= newLines.Count; j++)
                {
                    if (oldLines[i - 1] == newLines[j - 1])
                    {
                        lcs[i, j] = lcs[i - 1, j - 1] + 1;
                    }
                    else
                    {
                        lcs[i, j] = Math.Max(lcs[i - 1, j], lcs[i, j - 1]);
                    }
                }
            }

            return lcs;
        }

        private List<CodeLine> SplitIntoLines(string code)
        {
            if (string.IsNullOrEmpty(code))
                return new List<CodeLine>();

            var lines = code.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            return lines.Select((content, index) => new CodeLine
            {
                LineNumber = index + 1,
                Content = content,
                Height = 0,
                DiffType = DiffType.None,
                IsPlaceholder = false
            }).ToList();
        }

        public void OnGUI()
        {
            Rect availableArea = EditorGUILayout.GetControlRect(false, GUILayout.ExpandHeight(true));
            float totalWidth = availableArea.width;
            float halfWidth = (totalWidth - 4) / 2;
            float codeAreaWidth = halfWidth - LINE_NUMBER_WIDTH;

            if (Math.Abs(lastCalculatedWidth - codeAreaWidth) > 0.1f)
            {
                RecalculateLineHeights(oldCodeLines, codeAreaWidth);
                RecalculateLineHeights(newCodeLines, codeAreaWidth);
                lastCalculatedWidth = codeAreaWidth;
            }

            float totalContentHeight = Math.Max(
                oldCodeLines.Sum(l => l.Height),
                newCodeLines.Sum(l => l.Height)
            );

            // 开始整体的滚动视图
            Rect scrollViewRect = new Rect(availableArea.x, availableArea.y, totalWidth, availableArea.height);
            Rect contentRect = new Rect(0, 0, totalWidth - 16, totalContentHeight); // 16是滚动条宽度
            scrollPosition = GUI.BeginScrollView(scrollViewRect, scrollPosition, contentRect);

            // 左侧面板
            Rect leftPanelRect = new Rect(0, 0, halfWidth, totalContentHeight);
            DrawCodePanel("Original Code", oldCodeLines, leftPanelRect);

            // 分隔线
            Rect separatorRect = new Rect(halfWidth, 0, 4, totalContentHeight);
            EditorGUI.DrawRect(separatorRect, new Color(0.5f, 0.5f, 0.5f, 1));

            // 右侧面板
            Rect rightPanelRect = new Rect(halfWidth + 4, 0, halfWidth, totalContentHeight);
            DrawCodePanel("Generated Code", newCodeLines, rightPanelRect);

            GUI.EndScrollView();
        }

        private void DrawCodePanel(string title, List<CodeLine> lines, Rect panelRect)
        {
            // 绘制标题
            Rect headerRect = new Rect(panelRect.x, 0, panelRect.width, HEADER_HEIGHT);
            EditorGUI.LabelField(headerRect, title, headerStyle);

            float currentY = HEADER_HEIGHT;
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];

                // 绘制背景
                Rect lineRect = new Rect(panelRect.x, currentY, panelRect.width, line.Height);
                Color backgroundColor = GetBackgroundColor(line.DiffType, i % 2 == 0);
                EditorGUI.DrawRect(lineRect, backgroundColor);

                if (!line.IsPlaceholder)
                {
                    // 只为非空行显示行号
                    if (line.LineNumber > 0)
                    {
                        Rect lineNumberRect = new Rect(panelRect.x, currentY, LINE_NUMBER_WIDTH, line.Height);
                        EditorGUI.LabelField(lineNumberRect, line.LineNumber.ToString(), lineNumberStyle);
                    }

                    // 代码内容区域
                    Rect codeContentRect = new Rect(panelRect.x + LINE_NUMBER_WIDTH, currentY, panelRect.width - LINE_NUMBER_WIDTH, line.Height);
                    EditorGUI.LabelField(codeContentRect, line.Content, codeStyle);
                }

                currentY += line.Height;
            }
        }

        private Color GetBackgroundColor(DiffType diffType, bool isAlternateLine)
        {
            switch (diffType)
            {
                case DiffType.Added:
                    return AddedColor;
                case DiffType.Deleted:
                    return DeletedColor;
                case DiffType.Modified:
                    return ModifiedColor;
                case DiffType.Placeholder:
                    return Color.clear;
                default:
                    return isAlternateLine ? AlternateColor : Color.clear;
            }
        }

        private void RecalculateLineHeights(List<CodeLine> lines, float width)
        {
            foreach (var line in lines)
            {
                if (line.IsPlaceholder)
                {
                    line.Height = EditorGUIUtility.singleLineHeight;
                }
                else
                {
                    float height = codeStyle.CalcHeight(new GUIContent(line.Content), width);
                    line.Height = Mathf.Max(height, EditorGUIUtility.singleLineHeight);
                }
            }
        }
    }
}
