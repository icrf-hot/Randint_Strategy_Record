# Project Workflow

- Follow the existing Unity/uGUI and JSON data patterns. Preserve unrelated user changes.
- Default verification is C# compilation, not Play Mode or simulated gameplay.
- Starting or refreshing Unity is allowed when necessary to obtain compilation results.
- Only perform runtime tests or direct Unity gameplay verification when the user requests it or a specific issue requires it. State why before doing so.
- Scene/asset authoring through Editor APIs is implementation, not evidence of runtime correctness. Do not claim compilation proves gameplay or visual correctness.
- Target this project's Unity instance explicitly when using MCP/Pipeline. Never borrow another project's Editor.
- Update the wiki/TODO for completed work. Keep incomplete work and its handoff documented.
