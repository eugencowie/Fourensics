using System;
using UnityEngine;
using UnityEngine.UI;

class EditItemText : MonoBehaviour
{
    [SerializeField] InputField m_inputField = null;
    [SerializeField] Button m_submitButton = null;
    [SerializeField] Button m_cancelButton = null;

    public Action<string> OnSubmit = null;
    public Action OnCancel = null;

    public void CancelButtonPressed()
    {
        OnCancel?.Invoke();
    }

    public void SubmitButtonPressed()
    {
        OnSubmit?.Invoke(m_inputField?.text);
    }

    public void SetTextField(string text)
    {
        m_inputField.text = text;
    }
}
