using UnityEngine;
using TMPro;
using DG.Tweening;

public class TextWaveDOTween : MonoBehaviour
{
    [Header("Настройки волны")]
    public float amplitude = 5f;     // Высота волны
    public float frequency = 0.4f;   // Расстояние между пиками
    public float speed = 1f;         // Скорость движения волны

    private TMP_Text textMesh;
    private float timeCounter = 0f;

    void Awake()
    {
        textMesh = GetComponent<TMP_Text>();

        // Бесконечная анимация, просто вызывает UpdateWave()
        DOTween.To(() => 0f, x => UpdateWave(), 1f, 1f)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Incremental);
    }

    void UpdateWave()
    {
        if (!textMesh) return;

        // Увеличиваем время с учётом deltaTime
        timeCounter += Time.deltaTime * speed;

        textMesh.ForceMeshUpdate();
        var textInfo = textMesh.textInfo;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible)
                continue;

            var verts = textInfo.meshInfo[textInfo.characterInfo[i].materialReferenceIndex].vertices;
            int vertexIndex = textInfo.characterInfo[i].vertexIndex;

            // Плавная волна по всем буквам
            float wave = Mathf.Sin(timeCounter + i * frequency) * amplitude;

            for (int j = 0; j < 4; j++)
                verts[vertexIndex + j].y += wave;
        }

        // Применяем изменения к мешам
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textMesh.textInfo.meshInfo[i].mesh.vertices = textMesh.textInfo.meshInfo[i].vertices;
            textMesh.UpdateGeometry(textMesh.textInfo.meshInfo[i].mesh, i);
        }
    }
}