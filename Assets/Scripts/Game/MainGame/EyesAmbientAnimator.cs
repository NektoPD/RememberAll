// EyesAmbientAnimator.cs
using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections;

public class EyesAmbientAnimator : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerAvatarEyesDual eyes;
    [SerializeField] private TMP_Text leftEyeText;
    [SerializeField] private TMP_Text rightEyeText;

    [Header("Blink")]
    public float blinkDuration = 0.12f;   // скорость закрытия/открытия
    public Vector2 blinkInterval = new Vector2(2.5f, 6f); // паузы между миганиями

    [Header("Glow")]
    public Color glowA = new Color32(255, 213, 128, 255); // #FFD580
    public Color glowB = new Color32(255, 250, 224, 255); // #FFFAE0
    public float glowPeriod = 2.4f;

    [Header("Breath Scale (optional)")]
    public bool useBreath = true;
    public float breathScale = 1.03f;
    public float breathPeriod = 3.5f;

    private void Start()
    {
        // Пульс цвета (туда-обратно бесконечно)
        if (leftEyeText)  leftEyeText.DOColor(glowB, glowPeriod).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        if (rightEyeText) rightEyeText.DOColor(glowB, glowPeriod).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);

        // Лёгкое дыхание скейлом (оба глаза одинаково)
        if (useBreath)
        {
            if (leftEyeText)  leftEyeText.rectTransform
                .DOScale(breathScale, breathPeriod).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
            if (rightEyeText) rightEyeText.rectTransform
                .DOScale(breathScale, breathPeriod).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }

        // Запустим цикл мигания
        StartCoroutine(BlinkLoop());
    }

    private IEnumerator BlinkLoop()
    {
        var wait = new WaitForSeconds(Random.Range(blinkInterval.x, blinkInterval.y));
        while (true)
        {
            // закрываем
            yield return DOTween.To(v => eyes.SetBlink01(v), 0f, 1f, blinkDuration).SetEase(Ease.InQuad).WaitForCompletion();
            // открываем
            yield return DOTween.To(v => eyes.SetBlink01(v), 1f, 0f, blinkDuration * 1.2f).SetEase(Ease.OutQuad).WaitForCompletion();
            // пауза
            wait = new WaitForSeconds(Random.Range(blinkInterval.x, blinkInterval.y));
            yield return wait;
        }
    }
}
