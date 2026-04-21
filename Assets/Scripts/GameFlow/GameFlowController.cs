using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameFlowController : MonoBehaviour
{
    public static GameFlowController Instance { get; private set; }

    [Header("Timing")]
    [Tooltip("Seconds to stay in NextVisitor before looping back to WaitingForVisitor.")]
    [SerializeField] private float nextVisitorDelay = 1.5f;

    [Header("Documents")]
    [Tooltip("If true, instantiates documentPrefab when entering VisitorPresent. Turn off when documents come from VisitorSpawner (otherwise you get a second passport and stamp/delivery checks use the wrong instance).")]
    [SerializeField] private bool spawnDocumentPrefabOnVisitorPresent = true;
    [SerializeField] private GameObject documentPrefab;
    [SerializeField] private Transform spawnPoint;

    [Header("Visitor spawning (recommended)")]
    [Tooltip("If set, GameFlowController will ask this spawner to create the visitor + documents for the session.")]
    [SerializeField] private VisitorSpawner visitorSpawner;

    [Header("Inspector Hooks")]
    public UnityEvent onEnterWaitingForVisitor;
    public UnityEvent onEnterVisitorPresent;
    public UnityEvent onEnterDecision;
    public UnityEvent onEnterNextVisitor;

    // (previousState, newState)
    public static event Action<GameState, GameState> OnStateChanged;

    public GameState CurrentState { get; private set; }
    public bool LastDecisionApproved { get; private set; }

    private float stateTimer;
    private bool decisionApproved;
    private readonly List<DocumentController> activeDocuments = new();
    private readonly List<DocumentDeliverable> deliverables = new();
    private VisitorController activeVisitor;

    void Awake()
    {
        // Must run before any Start(): VisitorSpawner (via DayFlowController) can spawn + register
        // documents in its Start, and registration skips when Instance is still null.
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        TransitionTo(GameState.WaitingForVisitor);
    }

    /// <summary>
    /// Documents spawned before flow exists, or duplicate prefab spawns, may not be in this list.
    /// </summary>
    public bool IsSessionDeliverable(DocumentDeliverable d) => d != null && deliverables.Contains(d);

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        stateTimer += Time.deltaTime;

        if (CurrentState == GameState.NextVisitor && stateTimer >= nextVisitorDelay)
            TransitionTo(GameState.WaitingForVisitor);
    }

    // Called by Approve / Deny UI buttons
    public void SubmitDecision(bool approved)
    {
        if (CurrentState != GameState.VisitorPresent) return;
        decisionApproved = approved;
        LastDecisionApproved = approved;
        TransitionTo(GameState.Decision);
    }

    public void TransitionTo(GameState next)
    {
        GameState prev = CurrentState;
        ExitState(prev);
        CurrentState = next;
        stateTimer = 0f;
        OnStateChanged?.Invoke(prev, next);
        EnterState(next);
    }

    private void EnterState(GameState state)
    {
        switch (state)
        {
            case GameState.WaitingForVisitor:
                onEnterWaitingForVisitor.Invoke();
                // Immediately receive visitor (add delay/animation hook later)
                TransitionTo(GameState.VisitorPresent);
                break;

            case GameState.VisitorPresent:
                BeginVisitorSession();
                onEnterVisitorPresent.Invoke();
                break;

            case GameState.Decision:
                onEnterDecision.Invoke();
                // Immediately advance (add confirm button / animation hook later)
                TransitionTo(GameState.NextVisitor);
                break;

            case GameState.NextVisitor:
                onEnterNextVisitor.Invoke();
                // Timer ticks in Update; documents destroyed on exit
                break;
        }
    }

    private void ExitState(GameState state)
    {
        if (state == GameState.NextVisitor)
            DestroyActiveDocuments();
    }

    /// <summary>
    /// Call after spawning visitor documents elsewhere (e.g. VisitorSpawner) so stamp gates and delivery completion use the same instances.
    /// </summary>
    public void RegisterVisitorSessionDocument(GameObject doc)
    {
        if (doc == null) return;

        DocumentController controller = doc.GetComponent<DocumentController>();
        if (controller != null && !activeDocuments.Contains(controller))
            activeDocuments.Add(controller);

        DocumentDeliverable deliverable = doc.GetComponent<DocumentDeliverable>();
        if (deliverable != null && !deliverables.Contains(deliverable))
            deliverables.Add(deliverable);
    }

    private void SpawnDocuments()
    {
        if (!spawnDocumentPrefabOnVisitorPresent) return;
        if (documentPrefab == null || spawnPoint == null) return;

        GameObject doc = Instantiate(documentPrefab, spawnPoint.position, Quaternion.identity);
        RegisterVisitorSessionDocument(doc);
    }

    private void BeginVisitorSession()
    {
        // New session: reset tracking (documents will re-register as they spawn).
        activeDocuments.Clear();
        deliverables.Clear();

        if (visitorSpawner != null)
        {
            // Preferred: visitor + documents come from VisitorSpawner/DayManager rules.
            activeVisitor = visitorSpawner.SpawnVisitorForSession();
            return;
        }

        // Fallback: legacy single-document spawn (useful for quick prototyping).
        activeVisitor = null;
        SpawnDocuments();
    }

    private void DestroyActiveDocuments()
    {
        foreach (DocumentController doc in activeDocuments)
        {
            if (doc != null)
                Destroy(doc.gameObject);
        }
        activeDocuments.Clear();
        deliverables.Clear();
    }

    public void NotifyDocumentDelivered(DocumentDeliverable delivered)
    {
        if (delivered == null) return;
        if (CurrentState != GameState.VisitorPresent) return;
        if (!deliverables.Contains(delivered)) return;

        // Wait until all spawned deliverables are delivered.
        for (int i = 0; i < deliverables.Count; i++)
        {
            if (deliverables[i] != null && !deliverables[i].IsDelivered)
                return;
        }

        bool approved = ResolveApprovedFromDeliveredDocs();
        SubmitDecision(approved);
    }

    /// <summary>
    /// Global gate: delivery is allowed only after all required stampable documents have a stamp.
    /// "Required" here means: the document has a DocumentDeliverable that requires a stamp and also implements IStampable.
    /// </summary>
    public bool AreAllRequiredStampsPresent()
    {
        if (CurrentState != GameState.VisitorPresent) return false;
        if (deliverables.Count == 0) return false;

        for (int i = 0; i < deliverables.Count; i++)
        {
            DocumentDeliverable d = deliverables[i];
            if (d == null) continue;
            if (!d.RequiresStampIfStampable) continue;

            IStampable s = d.GetComponent<IStampable>();
            if (s == null) continue;

            if (s.Decision == StampDecision.None)
                return false;
        }

        return true;
    }

    private bool ResolveApprovedFromDeliveredDocs()
    {
        // Simple rule for now: if any stampable doc is Rejected -> denied, otherwise approved.
        // By delivery rules, stampable docs must have a stamp (Decision != None) to be deliverable.
        for (int i = 0; i < deliverables.Count; i++)
        {
            DocumentDeliverable d = deliverables[i];
            if (d == null) continue;
            IStampable s = d.GetComponent<IStampable>();
            if (s == null) continue;
            if (s.Decision == StampDecision.Rejected) return false;
        }
        return true;
    }
}
