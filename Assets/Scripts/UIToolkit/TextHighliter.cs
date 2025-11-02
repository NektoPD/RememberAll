using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class TextHighlighter : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Color _baseColor = new Color32(255, 213, 128, 255);
    public Color _highlightColor = new Color32(255, 250, 224, 255);

    public float _lerpSpeed = 8f;

    private TextMeshProUGUI _textComponent;
    private bool _isHovered = false;

    private void Awake()
    {
        _textComponent = GetComponent<TextMeshProUGUI>();
        _textComponent.color = _baseColor;
    }

    private void Update()
    {
        Color target = _isHovered ? _highlightColor : _baseColor;
        _textComponent.color = Color.Lerp(_textComponent.color, target, Time.deltaTime * _lerpSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
    }
}
