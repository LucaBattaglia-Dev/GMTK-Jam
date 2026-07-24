using UnityEngine;
using UnityEngine.UI; 
using TMPro; 
using UnityEngine.EventSystems;

public class HighlightButtonText : MonoBehaviour
{
    [SerializeField] private TMP_Text textToChange;
    [SerializeField] private Color color; //Color that text changes to on hover
    
    //public Color inactiveColor = Color.gray;
    private Color originalColor;

    void Start()
    {
        originalColor = textToChange.color;
    }

    public void OnPointerEnter(PointerEventData eventData){
        Debug.Log("HII");
        textToChange.color = color;
    }

    public void OnPointerExit(PointerEventData eventData){
        ResetColor(); 
    }
    
    public void ResetColor(){
        textToChange.color = originalColor; 
    }
}
