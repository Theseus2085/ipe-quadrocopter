# AGENTS.md — ipe-quadrocopter

Simon's discipline workspace for the university group project "Interdisziplinäre Produktentwicklung"
(TU Dresden, VPE chair, WiSe 2026/27): quadrocopter group project. Simon owns the ENTIRE software
("Prog" discipline); this folder supports his programming part.
This file overrides the parent `workspace/AGENTS.md` for everything under `ipe-quadrocopter/`.

## Subfolders / files

| File | What | Status |
|---|---|---|
| `harte-kriterien.md` | **BINDING COMPLIANCE RULES** — every hard criterion that can cost points (Lastenheft PROG-*, deliverables, deadlines, protocol rules). Read BEFORE any design/implementation work here. | Active — read first |
| `wissen-programmierung.md` | Knowledge base for the programming part, categorized (system architecture, UAVTalk, UAVObjects, libraries, WPF/threading, LibrePilot config, schedule, Beleg requirements). Extracted from the complete OPAL course mirror 2026-10-28. | Active — reference |
| `projekt-plan.md` | Project plan & status: current state, open points, plan (Simon generates), work log. | Active — check before working |

## Project rules

1. **COMPLIANCE GATE (binding, Simon 2026-10-28):** EVERY architecture, design, and implementation
   decision for this drone's software MUST be checked against `harte-kriterien.md` first and
   reasoned against it. If a decision/implementation/design would violate any rule in that file
   (especially FF requirements PROG-001..PROG-011A, deliverables C1–C10, deadlines D, protocol
   rules E, safety F): DO NOT BUILD IT. Refuse explicitly, tell Simon which rule (ID + source)
   is violated, and propose a compliant alternative. No silent deviations, no "creative" workarounds.
2. Course rules summary: C# / WPF only, provided libraries (KTCQuadroControl, vKTCQuadroControl)
   are mandatory, LibrePilot 16.09 firmware/config per chair instructions, UAVTalk protocol
   exactly as specified in VL5, two deliverable programs (virtual test env + real QC).
3. Hardware is fixed (CC3D Revolution, OPLink Mini, DJI Snail set, Xbox 360 controller) — no
   alternative components, no other firmware, no other comm stack.
4. The OPAL course updates during the semester (schedule, Lastenheft, CAD templates). When Simon
   says the mirror was updated: re-check `Wichtige Dokumente` + `Semesterablauf` nodes and update
   `harte-kriterien.md` + `wissen-programmierung.md` (keep the "Stand" date accurate).
5. OPAL interactions stay READ-ONLY (opal-access skill hard rules apply).
6. Language: course material and deliverable drafts are German; keep knowledge/rules files German.
7. All software Simon will run on his Windows PC (Visual Studio 2015/2017/2019). This machine only
   produces docs/plans/analysis — do not assume .NET tooling here.

## Standing rules (inherited from workspace index)

- Deliverables go to Nextcloud explicitly: MKCOL + PUT + HEAD-verify, then give the
  user the Web UI + WebDAV URLs. Never claim "synced" without a PUT.
- Local tree is source of truth. NC copy is a published snapshot via explicit PUT.
- Creds: grep from `backup.sh`, never source it. Uploads >100 MB fail with HTTP 413.
- Source documents live on the NC opal-mirror share:
  `opal-mirror/courses/Interdisziplinäre Produktentwicklung/...` (read-only reference).

## Notes

- Folder registered in `~/bin/nc_workspace_sync.py` FOLDERS (15-min sync).
- No channel permanently bound to this folder; work happens wherever Simon asks
  (this session: Discord #uni).
