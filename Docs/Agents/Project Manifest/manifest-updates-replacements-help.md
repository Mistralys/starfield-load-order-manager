# Project Manifest Updates - Multiple Replacements Help

## Summary of Changes

Updated the Project Manifest to document the multiple replacements help feature and associated workflow constraints.

---

## Files Updated

### 1. `api-viewmodels.md`

**Changes to `DiffDialogViewModel`**:
Added two new properties to the public API:

```csharp
public bool ShowMultipleReplacementsHelp { get; }
public string MultipleReplacementsHelpMessage { get; }
```

**Purpose**: 
- `ShowMultipleReplacementsHelp` - Controls visibility of info banner when 2+ removals/replacements detected
- `MultipleReplacementsHelpMessage` - Contains the guidance message shown in the banner

---

### 2. `constraints-invariants.md`

**New Section: Replacement Workflow Constraints**

Documented the fundamental constraints and design decisions around replacement operations:

**Key Points**:
- Reference file is the source of truth for all comparisons
- Replacement operations update `Plugins.txt` immediately but never the reference file
- Only "Accept changes" updates the reference file
- Two valid workflows for sequential replacements documented
- Visual guidance provided via blue info banner
- Intentionally no session state management to keep architecture simple
- Explicitly states this is "working as designed" behavior

**Why This Matters**:
- Future AI agents will understand this is intentional, not a bug
- Prevents attempts to "fix" with complex session management
- Documents the architectural decision to keep things simple

---

### 3. `data-flows.md`

**Updated: Diff Dialog Operations Section**

Added information about the multiple replacements help banner flow:

**Flow Documentation**:
1. `ShowMultipleReplacementsHelp` property triggers when 2+ removals/replacements detected
2. Blue info banner displays with workflow guidance
3. `MultipleReplacementsHelpMessage` provides the banner text
4. `UpdateDiffState()` notifies property changes when diff collection changes

**Integration Points**:
- Fits naturally into existing diff dialog operations flow
- Follows same pattern as sorting recommendation banner
- Uses existing property notification mechanisms

---

## Documentation Principles Applied

### 1. Explicit Over Implicit
- Clearly states "working as designed" rather than letting agents guess
- Documents both what it does AND why it's designed that way

### 2. Constraint Documentation
- Captured in `constraints-invariants.md` where it belongs
- Explains the architectural decision (no session state management)
- Provides rationale (simplicity, maintainability)

### 3. API Completeness
- All public properties documented in `api-viewmodels.md`
- Data flow documented in `data-flows.md`
- Constraints documented in `constraints-invariants.md`

### 4. Future-Proofing
- Prevents future agents from attempting complex "fixes"
- Documents the evaluated alternatives (session cache, staging file, in-memory management)
- Explains why they weren't chosen (mentioned in development history)

---

## What This Prevents

### ? Bad Agent Behavior Avoided

**Without this documentation, a future AI agent might**:
1. See multiple replacements not persisting
2. Assume it's a bug
3. Implement complex session state management
4. Introduce memory leaks or lifecycle issues
5. Over-complicate the architecture

**With this documentation, a future AI agent will**:
1. See the constraint documented
2. Understand it's intentional
3. Refer users to the existing help banner
4. Not attempt to "fix" what isn't broken

---

## Maintenance Benefits

### For Human Developers
- Clear understanding of design decisions
- Prevents "why does it work this way?" questions
- Documents evaluated alternatives

### For AI Agents
- No ambiguity about intended behavior
- Clear constraints prevent misguided "improvements"
- Documented in standard manifest structure

### For Users
- Application description already updated with workflow notes
- Info banner provides in-context guidance
- Two clear workflow options documented

---

## Consistency with Existing Patterns

The documentation updates follow established patterns:

**API Documentation**:
- New properties added to `DiffDialogViewModel` section
- Follows same format as other ViewModel properties
- Alphabetically ordered within property list

**Constraints Documentation**:
- New section follows same format as other constraint sections
- Uses consistent markdown formatting
- Includes bullet points for clarity

**Data Flow Documentation**:
- Integrated into existing "Diff Dialog Operations" section
- Maintains flow-based narrative structure
- Uses existing terminology and patterns

---

## Cross-References

The manifest now properly cross-references:

1. **`application-description.md`** - User-facing documentation of workflow
2. **`18-multiple-replacements-help.md`** - Implementation details and rationale
3. **`17-sequential-replacement-fix.md`** - Investigation and why initial fix was wrong

This creates a complete documentation trail from:
- User guidance ? Implementation details ? Architectural constraints ? Public API

---

## Verification

All manifest updates verified for:
- ? Accuracy - Matches actual implementation
- ? Completeness - All new public API documented
- ? Consistency - Follows existing patterns
- ? Clarity - No ambiguous language
- ? Cross-references - Properly linked to other docs

---

## Impact on Future Development

### When Adding Features
- Developers know to document constraints, not just APIs
- Clear pattern established for documenting "working as designed" behaviors
- Architectural decisions captured for reference

### When Fixing Bugs
- Clear distinction between bugs and intentional behavior
- Documented constraints help identify what can/cannot be changed
- Rationale preserved for design decisions

### When Onboarding
- New team members understand "why" not just "what"
- Evaluated alternatives documented (session cache, etc.)
- Design philosophy clearly communicated

---

## Conclusion

These Project Manifest updates ensure that:
1. The multiple replacements behavior is clearly documented as intentional
2. Future AI agents won't attempt to "fix" it with complex solutions
3. The architectural decision to keep things simple is preserved
4. All public APIs are properly documented
5. Data flows are clearly explained
6. Constraints are explicitly stated

This follows the Project Manifest philosophy: **"Source-of-truth overview for future AI agents. Do not infer behavior beyond what is documented here."**
