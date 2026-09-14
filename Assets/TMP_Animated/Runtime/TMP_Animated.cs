using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TMPro
{
    public enum Emotion { happy, sad, suprised, angry };
    [System.Serializable] public class EmotionEvent : UnityEvent<Emotion> { }

    [System.Serializable] public class ActionEvent : UnityEvent<string> { }

    [System.Serializable] public class TextRevealEvent : UnityEvent<char> { }

    [System.Serializable] public class DialogueEvent : UnityEvent { }

    public class TMP_Animated : TextMeshProUGUI
    {

        [SerializeField] private float speed = 10;
        public EmotionEvent onEmotionChange;
        public ActionEvent onAction;
        public TextRevealEvent onTextReveal;
        public DialogueEvent onDialogueFinish;

        private Coroutine readRoutine;

        /// <summary>是否正在逐字显示文本。</summary>
        public bool IsReading { get; private set; }

        public void ReadText(string newText)
        {
            // 若上一段还在播放，先停止，避免两段文字叠加
            if (readRoutine != null)
                StopCoroutine(readRoutine);

            text = string.Empty;
            // split the whole text into parts based off the <> tags 
            // even numbers in the array are text, odd numbers are tags
            string[] subTexts = newText.Split('<', '>');

            // textmeshpro still needs to parse its built-in tags, so we only include noncustom tags
            string displayText = "";
            for (int i = 0; i < subTexts.Length; i++)
            {
                if (i % 2 == 0)
                    displayText += subTexts[i];
                else if (!isCustomTag(subTexts[i].Replace(" ", "")))
                    displayText += $"<{subTexts[i]}>";
            }
            // check to see if a tag is our own
            bool isCustomTag(string tag)
            {
                return tag.StartsWith("speed=") || tag.StartsWith("pause=") || tag.StartsWith("emotion=") || tag.StartsWith("action");
            }

            // send that string to textmeshpro and hide all of it, then start reading
            text = displayText;
            maxVisibleCharacters = 0;
            readRoutine = StartCoroutine(Read());

            IEnumerator Read()
            {
                IsReading = true;
                int subCounter = 0;
                int visibleCounter = 0;
                while (subCounter < subTexts.Length)
                {
                    // if 
                    if (subCounter % 2 == 1)
                    {
                        yield return EvaluateTag(subTexts[subCounter].Replace(" ", ""));
                    }
                    else
                    {
                        while (visibleCounter < subTexts[subCounter].Length)
                        {
                            onTextReveal?.Invoke(subTexts[subCounter][visibleCounter]);
                            visibleCounter++;
                            maxVisibleCharacters++;
                            yield return new WaitForSeconds(1f / speed);
                        }
                        visibleCounter = 0;
                    }
                    subCounter++;
                }
                yield return null;

                WaitForSeconds EvaluateTag(string tag)
                {
                    if (tag.Length > 0)
                    {
                        if (tag.StartsWith("speed="))
                        {
                            speed = float.Parse(tag.Split('=')[1]);
                        }
                        else if (tag.StartsWith("pause="))
                        {
                            return new WaitForSeconds(float.Parse(tag.Split('=')[1]));
                        }
                        else if (tag.StartsWith("emotion="))
                        {
                            onEmotionChange?.Invoke((Emotion)System.Enum.Parse(typeof(Emotion), tag.Split('=')[1]));
                        }
                        else if (tag.StartsWith("action="))
                        {
                            onAction?.Invoke(tag.Split('=')[1]);
                        }
                    }
                    return null;
                }
                IsReading = false;
                readRoutine = null;
                onDialogueFinish?.Invoke();
            }
        }

        /// <summary>立即显示全部文字（跳过打字动画），例如玩家点击加速时调用。</summary>
        public void SkipToEnd()
        {
            if (readRoutine == null)
                return;

            StopCoroutine(readRoutine);
            readRoutine = null;
            maxVisibleCharacters = textInfo.characterCount > 0 ? textInfo.characterCount : text.Length;
            IsReading = false;
            onDialogueFinish?.Invoke();
        }
    }
}