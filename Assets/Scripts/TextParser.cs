using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Animated))]
public class TextParser : MonoBehaviour
{
    [TextArea] public string masterText;
    private TMP_Animated animatedText;

    private void Awake()
    {
        animatedText = GetComponent<TMP_Animated>();
    }
}
