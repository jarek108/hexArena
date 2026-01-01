# Agent Interaction Protocols

## Roles
1.  **ARCHITECT** (Planner, Tech Lead, Orchestrator)
2.  **CODER** (Implementer, Self-Verifier)
3.  **QA ENGINEERING** (Scope Auditor, Test Critic)
4.  **AUTOMATED SYSTEM** (The "Script": Runs Tests/Checks Logs)

## Interaction Matrix

### 1. Human <-> Architect
*   **[REQ] Vision & Architecture**
    *   **Action**: Negotiate high-level vision, define architecture, breakdown into `workplan_plan.md` with dependencies.
    *   **Artifacts**: `workplan_plan.md` (Updated).

*   **[REP] Report / Inspection**
    *   **Action**: Architect presents `walkthrough.md`, `workplan_plan.md` status, or verify logs.
    *   **Artifacts**: `walkthrough.md`, `status_report.md` (Optional).

### 2. Architect -> Coder
*   **[ASSIGN] Implement Feature**
    *   **Signal**: `handshake.role = "CODER"`, `handshake.task = "Implement <Feature>"`.
    *   **Artifacts**: `implementation_plan.md` (Created/Updated by Architect with high-level design).

### 3. Coder <-> Automated System
*   **[EXEC] Verification Request**
    *   **From**: Coder
    *   **To**: Automated System
    *   **Trigger**: Implementation Draft Complete.
    *   **Action**: Run `python tools/verify_build.py` (Checks Logs & Test Results).
*   **[RES] Verification Result**
    *   **From**: Automated System
    *   **To**: Coder
    *   **Result**: 
        *   **FAIL**: Script exits 1. Coder must fix.
        *   **PASS**: Script exits 0. Coder proceeds.
    *   **Artifacts**: `verification_log.txt`.

### 4. Coder -> Architect
*   **[BLOCK] Clarification Needed**
    *   **Signal**: `handshake.role = "ARCHITECT"`, `handshake.status = "BLOCKED"`.
    *   **Artifacts**: `blocker_report.md` (Context, Error Logs, Ambiguity details).

### 5. Coder -> QA Engineering
*   **[HANDOFF] Implementation Verified**
    *   **Trigger**: Script returns **PASS**.
    *   **Signal**: `handshake.role = "QA"`, `handshake.status = "READY_TO_REVIEW"`.
    *   **Artifacts**: `implementation_plan.md` (As-Built), `verification_log.txt` (Proof of Pass).

### 6. QA Engineering -> Architect
*   **[APPROVE] Feature Validated**
    *   **Trigger**: Scope matches Spec AND Tests are sufficient.
    *   **Signal**: `handshake.role = "ARCHITECT"`, `handshake.status = "APPROVED"`.
    *   **Artifacts**: `validation_report.md`.

*   **[BLOCK] Scope/Quality Gap**
    *   **Trigger**: Coder missed requirements OR wrote "Cheating" tests.
    *   **Signal**: `handshake.role = "ARCHITECT"`, `handshake.status = "BLOCKED"`.
    *   **Artifacts**: `qa_gap_analysis.md`.

### 7. QA Engineering -> Coder
*   **[REJECT] Scope Mismatch / Bad Tests**
    *   **Trigger**: Implementation doesn't match Design OR Tests are sparse.
    *   **Signal**: `handshake.role = "CODER"`, `handshake.status = "DEFECT"`.
    *   **Artifacts**: `defect_report.md`.
