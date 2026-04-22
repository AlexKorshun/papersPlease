using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisitorController : MonoBehaviour
{
    [SerializeField] private PersonArchetypeSO archetype;
    [SerializeField] private List<MonoBehaviour> documents = new();
    [SerializeField] private VisitorProfile profile;

    [Header("Animation (Animator triggers)")]
    [SerializeField] private Animator animator;
    [Tooltip("Visitor walks into the booth (spawn / session start).")]
    [SerializeField] private string triggerEnterInside = "EnterInside";
    [Tooltip("Visitor leaves after rejection (player said no).")]
    [SerializeField] private string triggerExitBack = "ExitBack";
    [Tooltip("Visitor leaves after approval (player said yes).")]
    [SerializeField] private string triggerExitForward = "ExitForward";
     
    [Header("Speech (scene UI)")]
    [SerializeField] private VisitorSpeechUI speechUI;
    [Tooltip("Lines shown after the visitor has walked in (in order, replacing each other in the same UI spot).")]
    [SerializeField] private List<string> linesAfterEnter = new() { "Ваши документы.", "Цель прибытия?" };
    [TextArea]
    [SerializeField] private string lineAfterApproved = "Спасибо.";
    [TextArea]
    [SerializeField] private string lineAfterRejected = "Почему?!";

    [Header("Documents (after enter animation)")]
    [Tooltip("If true, SpawnDocuments runs only after OnEnterApproachAnimationFinished() (Animation Event) or fallback timeout.")]
    [SerializeField] private bool waitForEnterAnimationBeforeDocuments = true;

    [Tooltip("If enter animation never signals completion, documents spawn after this many seconds.")]
    [SerializeField] private float enterAnimationFallbackSeconds = 10f;

    [Header("Lifecycle")]
    [Tooltip("Delay before Destroy when flow enters NextVisitor (should cover exit walk animation).")]
    [SerializeField] private float destroyDelayAfterNextVisitor = 2f;

    private bool exitAnimationTriggered;
    private bool enterApproachFinished;
    private Coroutine documentSpawnWaitRoutine;

    public PersonArchetypeSO Archetype => archetype;
    public VisitorProfile Profile => profile;

    public void Initialize(PersonArchetypeSO personArchetype, VisitorProfile visitorProfile)
    {
        archetype = personArchetype;
        profile = visitorProfile;
        documents.Clear();
        exitAnimationTriggered = false;
        enterApproachFinished = false;

        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        PlayEnterInside();
    }

    /// <summary>
    /// Call from an Animation Event on the "enter / walk inside" clip when the visitor has finished approaching.
    /// </summary>
    public void OnEnterApproachAnimationFinished()
    {
        enterApproachFinished = true;
        ShowSpeechSequence(linesAfterEnter);
    }

    /// <summary>
    /// Schedules document spawn after the enter animation (or immediately if wait is disabled).
    /// Pass a <b>copy</b> of the rolled list — the spawner buffer is reused each visitor.
    /// </summary>
    public void BeginSpawnDocumentsAfterEnter(
        DayManager dayManager,
        DocumentPrefabRegistrySO documentPrefabs,
        Transform documentSpawnPoint,
        List<DayManager.RolledDocument> rolledDocumentsCopy)
    {
        StopDocumentSpawnRoutine();
        if (rolledDocumentsCopy == null || rolledDocumentsCopy.Count == 0) return;

        documentSpawnWaitRoutine = StartCoroutine(DocumentsAfterEnterRoutine(dayManager, documentPrefabs, documentSpawnPoint, rolledDocumentsCopy));
    }

    private void StopDocumentSpawnRoutine()
    {
        if (documentSpawnWaitRoutine != null)
        {
            StopCoroutine(documentSpawnWaitRoutine);
            documentSpawnWaitRoutine = null;
        }
    }

    private IEnumerator DocumentsAfterEnterRoutine(
        DayManager dayManager,
        DocumentPrefabRegistrySO documentPrefabs,
        Transform documentSpawnPoint,
        List<DayManager.RolledDocument> rolledDocumentsCopy)
    {
        if (waitForEnterAnimationBeforeDocuments)
        {
            float t = 0f;
            float limit = Mathf.Max(0.05f, enterAnimationFallbackSeconds);
            while (!enterApproachFinished && t < limit)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }
        // If we never got the animation event, still show the enter line once.
        if (!enterApproachFinished)
            ShowSpeechSequence(linesAfterEnter);

        SpawnDocuments(dayManager, documentPrefabs, documentSpawnPoint, rolledDocumentsCopy);
        documentSpawnWaitRoutine = null;
    }

    private void OnDestroy()
    {
        StopDocumentSpawnRoutine();
    }

    public void AddDocumentComponent(MonoBehaviour docComponent)
    {
        if (docComponent == null) return;
        documents.Add(docComponent);
    }

    /// <summary>
    /// Spawns and initializes rolled documents for this visitor, using the shared VisitorProfile
    /// so all documents stay consistent (name, birth date, photo, etc).
    /// </summary>
    public void SpawnDocuments(
        DayManager dayManager,
        DocumentPrefabRegistrySO documentPrefabs,
        Transform documentSpawnPoint,
        List<DayManager.RolledDocument> rolledDocuments)
    {
        if (rolledDocuments == null || rolledDocuments.Count == 0) return;
        if (documentPrefabs == null) return;

        string today = dayManager != null ? dayManager.CurrentDateString : "21.10.2077";
        Vector3 basePos = documentSpawnPoint != null ? documentSpawnPoint.position : transform.position;

        for (int i = 0; i < rolledDocuments.Count; i++)
        {
            DayManager.RolledDocument rolled = rolledDocuments[i];
            DocumentTypeSO type = rolled.DocumentType;
            GameObject prefab = documentPrefabs.GetPrefab(type);
            if (prefab == null) continue;

            Vector3 p = basePos + new Vector3(0.12f * i, -0.06f * i, 0f);
            GameObject go = Instantiate(prefab, p, Quaternion.identity);

            PassportDocument passport = go.GetComponent<PassportDocument>();
            if (passport != null)
                passport.Initialize(type, rolled.IsForged, VisitorProfileGenerator.BuildPassportData(Profile, i, rolled.IsForged));

            EntryPermitDocument permit = go.GetComponent<EntryPermitDocument>();
            if (permit != null)
                permit.Initialize(type, rolled.IsForged, VisitorProfileGenerator.BuildPermitData(Profile, today, i, rolled.IsForged));

            MonoBehaviour mb = go.GetComponent<MonoBehaviour>();
            if (mb != null)
                AddDocumentComponent(mb);

            if (GameFlowController.Instance != null)
                GameFlowController.Instance.RegisterVisitorSessionDocument(go);
        }
    }

    public void SetShouldBeAllowed(bool value)
    {
        profile.ShouldBeAllowed = value;
    }

    public IReadOnlyList<MonoBehaviour> DocumentComponents => documents;

    private void OnEnable()
    {
        GameFlowController.OnStateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        GameFlowController.OnStateChanged -= OnGameStateChanged;
        CancelInvoke(nameof(DestroySelf));
        StopDocumentSpawnRoutine();
        if (speechUI != null)
            speechUI.ClearSpawnedLines();
    }

    private void OnGameStateChanged(GameState prev, GameState next)
    {
        if (next == GameState.Decision)
        {
            bool playerApproved = GameFlowController.Instance != null && GameFlowController.Instance.LastDecisionApproved;
            bool correct = playerApproved == profile.ShouldBeAllowed;

            if (ScoreManager.Instance != null)
            {
                if (correct) ScoreManager.Instance.AddScore();
                else         ScoreManager.Instance.AddPenalty();
            }

            // Use sequence so it works both in single-text mode and stack (prefab) mode.
            ShowSpeechSequenceSingle(playerApproved ? lineAfterApproved : lineAfterRejected);
            PlayExitByApproval(playerApproved);
        }

        if (next == GameState.NextVisitor)
        {
            CancelInvoke(nameof(DestroySelf));
            float d = Mathf.Max(0.05f, destroyDelayAfterNextVisitor);
            Invoke(nameof(DestroySelf), d);
        }
    }

    private void DestroySelf()
    {
        Destroy(gameObject);
    }

    private void PlayEnterInside()
    {
        if (animator == null) return;
        TryResetTrigger(animator, triggerExitBack);
        TryResetTrigger(animator, triggerExitForward);
        TrySetTrigger(animator, triggerEnterInside);
    }

    private void PlayExitByApproval(bool playerApproved)
    {
        if (exitAnimationTriggered) return;
        exitAnimationTriggered = true;

        if (animator == null) return;

        TryResetTrigger(animator, triggerEnterInside);

        if (playerApproved)
        {
            TryResetTrigger(animator, triggerExitBack);
            TrySetTrigger(animator, triggerExitForward);
        }
        else
        {
            TryResetTrigger(animator, triggerExitForward);
            TrySetTrigger(animator, triggerExitBack);
        }
    }

    private void ShowSpeech(string line)
    {
        if (string.IsNullOrEmpty(line)) return;
        if (speechUI == null)
            speechUI = FindFirstObjectByType<VisitorSpeechUI>();
        if (speechUI != null)
            speechUI.Show(line);
    }

    private void ShowSpeechSequence(IReadOnlyList<string> lines)
    {
        if (lines == null || lines.Count == 0) return;
        if (speechUI == null)
            speechUI = FindFirstObjectByType<VisitorSpeechUI>();
        if (speechUI != null)
            speechUI.PlaySequence(lines);
    }

    private void ShowSpeechSequenceSingle(string line)
    {
        if (string.IsNullOrEmpty(line)) return;
        if (speechUI == null)
            speechUI = FindFirstObjectByType<VisitorSpeechUI>();
        if (speechUI != null)
            speechUI.PlaySequence(new[] { line });
    }

    private static bool HasTriggerParameter(Animator anim, string name)
    {
        if (anim == null || string.IsNullOrEmpty(name) || anim.runtimeAnimatorController == null) return false;
        for (int i = 0; i < anim.parameters.Length; i++)
        {
            AnimatorControllerParameter p = anim.parameters[i];
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == name)
                return true;
        }
        return false;
    }

    private static void TryResetTrigger(Animator anim, string name)
    {
        if (!HasTriggerParameter(anim, name)) return;
        anim.ResetTrigger(name);
    }

    private static void TrySetTrigger(Animator anim, string name)
    {
        if (!HasTriggerParameter(anim, name)) return;
        anim.SetTrigger(name);
    }
}

