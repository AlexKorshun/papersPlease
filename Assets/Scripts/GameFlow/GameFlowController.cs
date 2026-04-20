using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameFlowController : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("Seconds to stay in NextVisitor before looping back to WaitingForVisitor.")]
    [SerializeField] private float nextVisitorDelay = 1.5f;

    [Header("Documents")]
    [SerializeField] private GameObject documentPrefab;
    [SerializeField] private Transform spawnPoint;

    [Header("Inspector Hooks")]
    public UnityEvent onEnterWaitingForVisitor;
    public UnityEvent onEnterVisitorPresent;
    public UnityEvent onEnterDecision;
    public UnityEvent onEnterNextVisitor;

    // (previousState, newState)
    public static event Action<GameState, GameState> OnStateChanged;

    public GameState CurrentState { get; private set; }

    private float stateTimer;
    private bool decisionApproved;
    private readonly List<DocumentController> activeDocuments = new();

    void Start()
    {
        TransitionTo(GameState.WaitingForVisitor);
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
                SpawnDocuments();
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

    private void SpawnDocuments()
    {
        if (documentPrefab == null || spawnPoint == null) return;

        GameObject doc = Instantiate(documentPrefab, spawnPoint.position, Quaternion.identity);
        DocumentController controller = doc.GetComponent<DocumentController>();
        if (controller != null)
            activeDocuments.Add(controller);
    }

    private void DestroyActiveDocuments()
    {
        foreach (DocumentController doc in activeDocuments)
        {
            if (doc != null)
                Destroy(doc.gameObject);
        }
        activeDocuments.Clear();
    }
}
