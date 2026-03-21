using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LayoutTester : MonoBehaviour
{
    public TMP_Text targetText;
    public TMP_InputField inputField;
    public Button button;

    private void OnEnable()
    {
        button.onClick.AddListener(Test);
    }
    private void OnDisable()
    {
        button.onClick.RemoveListener(Test);
    }

    public void Test()
    {
        targetText.text = inputField.text;
    }
}
