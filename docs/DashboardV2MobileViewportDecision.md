# Dashboard V2 Mobile Viewport Decision

## Status

Approved product decision, recorded on 2026-11-17.

## Decision

1. The Mobile Dashboard does not require every Summary section to fit within
   the first viewport.
2. Preserve the Summary order: **Financial Overview → Budget Radar → Goal
   Summary**.
3. Scrolling through these sections is expected UX and is not a defect.
4. Keep **Recent Activity** and **Account Details** as progressive disclosure:
   collapsed by default on mobile, with their content available on expansion.
5. This decision does not authorize or require changes to React, CSS, or API
   behavior.
