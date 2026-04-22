using System.Collections;
using TMPro;
using UnityEngine;

public class IntroUI : MonoBehaviour
{
    [SerializeField] private TMP_Text narrativeText;
    [SerializeField] private TMP_Text hintText;

    [TextArea(4, 10)]
    [SerializeField] private string narrativeContent =
        "Аркия. 23 октября 2077 года.\n\n" +
        "Вы — инспектор пограничного контроля.\n" +
        "Ваша задача — проверять документы прибывающих и решать, кого пропустить, а кого нет.\n\n" +
        "Будьте внимательны. Ошибки стоят дорого.";

    [SerializeField] private float typewriterSpeed = 0.04f;
    [Tooltip("Минимальное время до того как можно нажать (в секундах).")]
    [SerializeField] private float minWaitBeforeClick = 1.5f;

    private bool canProceed;
    private bool proceeded;

    private void Start()
    {
        if (hintText != null)
            hintText.gameObject.SetActive(false);

        StartCoroutine(TypewriterRoutine());
        StartCoroutine(EnableClickAfterDelay());
    }

    private void Update()
    {
        if (!canProceed || proceeded) return;

        if (Input.anyKeyDown)
            Proceed();
    }

    private IEnumerator TypewriterRoutine()
    {
        narrativeText.text = "";
        foreach (char c in narrativeContent)
        {
            narrativeText.text += c;
            yield return new WaitForSeconds(typewriterSpeed);
        }
    }

    private IEnumerator EnableClickAfterDelay()
    {
        yield return new WaitForSeconds(minWaitBeforeClick);
        canProceed = true;

        if (hintText != null)
            hintText.gameObject.SetActive(true);
    }

    private void Proceed()
    {
        proceeded = true;
        SceneFlowManager.Instance.StartFirstDay();
    }
}
