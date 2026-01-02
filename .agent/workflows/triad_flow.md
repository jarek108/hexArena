---
description: Triad Architect-Coder-QA Workflow
---

# Triad Workflow

## Phase 1: Router
// turbo
1. Read `.agent/workflow_handshake.json`.
2. Inspect `current_role`:
   - `ARCHITECT` (or MANAGER) -> GOTO Phase 2.
   - `CODER` -> GOTO Phase 3.
   - `QA` -> GOTO Phase 4.

## Phase 2: ARCHITECT (Vision, Orchestration, Test Eng)
1. **Mode**: `PLANNING`.
2. **Context**:
   - IF **Start/User Request**:
      - define Architecture/Approach.
      - Update `workplan_plan.md` (Set Dependencies).
      - **Action**: Pick safe, unblocked feature.
      - **Artifact**: `implementation_plan.md` (Draft).
      - **Handoff**: `role: "CODER"`, `status: "IMPLEMENT"`.
   - IF **Code Complete (from Coder)**:
      - Review Implementation (High Level).
      - **Handoff**: `role: "QA"`, `status: "READY_TO_TEST"`.
   - IF **Blocked (from Coder/QA)**:
      - Analyze Blocker (e.g., "Missing Tests").
      - IF resolveable -> Update Spec -> `role: "CODER"`.
      - IF ambiguous -> Ask Human -> Wait -> `role: "CODER"`.
   - IF **Approved (from QA)**:
      - Mark `workplan_plan.md` feature as `[x]`.
      - **Action**: Update/Generate `walkthrough.md` (Report).
      - **Action**: Run `python tools/archive_cycle.py --feature "Name" --status "APPROVED" --cycles N`.
      - **Orchestrate**: Check what implicitly unblocks. Pick next.

## Phase 3: CODER (Implementation & Verification)
1. **Mode**: `EXECUTION`.
2. **Action**: Implement Feature.
3. **Loop**:
   - Run `python tools/verify_build.py`.
   - **IF FAIL**: Fix Console/Test errors -> Retry Loop.
   - **IF PASS**: Generate `verification_log.txt` -> **Handoff** to QA.
4. **Handoff**:
   - `role: "QA"`, `status: "READY_TO_REVIEW"`.

## Phase 4: QA ENGINEERING (Audit & Validation)
1. **Mode**: `VERIFICATION` (Scope Audit).
2. **Context**: Tests are *already green* (proven by Coder's logs).
3. **Checks**:
   - Does Implementation match `workplan_plan.md` Scope?
   - Are the tests sufficient (coverage)?
   - Are the tests valid (no false positives)?
4. **Verdict**:
   - **Bad Scope/Tests** -> `role: "CODER"`, `status: "DEFECT"`, **Artifact**: `defect_report.md`.
   - **Bad Architecture** -> `role: "ARCHITECT"`, `status: "BLOCKED"`, **Artifact**: `qa_gap_analysis.md`.
   - **Pass** -> `role: "ARCHITECT"`, `status: "APPROVED"`, **Artifact**: `validation_report.md`.
