using System.Diagnostics;
using System.IO;
using LoadOrderKeeper.Models;

namespace LoadOrderKeeper.Services
{
    public static class DiffService
    {
        private static readonly ViewTexts.LocalizationService _localization = ViewTexts.LocalizationService.Instance;

        public static async Task<IReadOnlyList<DiffLineModel>> GetPluginsDiffAsync(AppConfigModel config)
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (!config.IsValid())
            {
                throw new InvalidOperationException("Configuration paths are invalid.");
            }

            string targetPath = config.GetPluginsFilePath();
            string referencePath = config.GetReferenceFilePath();

            if (!File.Exists(referencePath))
            {
                throw new FileNotFoundException("Reference file not found.", referencePath);
            }

            if (!File.Exists(targetPath))
            {
                throw new FileNotFoundException("Plugins file not found.", targetPath);
            }

            var referenceMods = await FileService.ReadModListAsync(referencePath, isReferenceFile: true).ConfigureAwait(false);
            var currentMods   = await FileService.ReadModListAsync(targetPath).ConfigureAwait(false);

            var refFileNames = referenceMods.Select(m => m.FileName).ToList();
            var curFileNames = currentMods.Select(m => m.FileName).ToList();

            var lcs = ComputeLcs(refFileNames, curFileNames, StringComparer.OrdinalIgnoreCase);

            return ClassifyChanges(referenceMods, currentMods, lcs);
        }

        /// <summary>
        /// Checks if there are any moved mods that are NOT part of dependent change lists.
        /// These independent moves indicate external reordering that sorting could fix.
        /// </summary>
        public static async Task<bool> HasIndependentMovedModsAsync(AppConfigModel config)
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (!config.IsValid())
            {
                return false;
            }

            string targetPath = config.GetPluginsFilePath();
            string referencePath = config.GetReferenceFilePath();

            if (!File.Exists(referencePath) || !File.Exists(targetPath))
            {
                return false;
            }

            var diffLines = await GetPluginsDiffAsync(config).ConfigureAwait(false);
            
            // Get all mods that are part of dependent change lists
            var dependentMods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in diffLines)
            {
                foreach (var dependent in line.DependentChanges)
                {
                    dependentMods.Add(dependent.FileName);
                }
            }
            
            // Check if there are any moved mods that are NOT in the dependent set
            bool hasIndependentMoves = diffLines.Any(line => 
                line.ChangeType == DiffChangeType.Moved && 
                !dependentMods.Contains(line.FileName));
            
            return hasIndependentMoves;
        }

        /// <summary>
        /// Computes the Longest Common Subsequence of two string lists using a standard
        /// dynamic-programming approach. Returns a list of paired (refIndex, curIndex) tuples
        /// representing the LCS in order, where refIndex is the position in
        /// <paramref name="reference"/> and curIndex is the position in <paramref name="current"/>.
        /// </summary>
        internal static List<(int refIndex, int curIndex)> ComputeLcs(
            IReadOnlyList<string> reference,
            IReadOnlyList<string> current,
            StringComparer comparer)
        {
            int m = reference.Count;
            int n = current.Count;

            // Build DP table
            var dp = new int[m + 1, n + 1];
            for (int i = 1; i <= m; i++)
            {
                for (int j = 1; j <= n; j++)
                {
                    if (comparer.Equals(reference[i - 1], current[j - 1]))
                    {
                        dp[i, j] = dp[i - 1, j - 1] + 1;
                    }
                    else
                    {
                        dp[i, j] = Math.Max(dp[i - 1, j], dp[i, j - 1]);
                    }
                }
            }

            // Backtrack to recover the LCS pairs
            var result = new List<(int refIndex, int curIndex)>(dp[m, n]);
            int ri = m, ci = n;
            while (ri > 0 && ci > 0)
            {
                if (comparer.Equals(reference[ri - 1], current[ci - 1]))
                {
                    result.Add((ri - 1, ci - 1));
                    ri--;
                    ci--;
                }
                else if (dp[ri - 1, ci] >= dp[ri, ci - 1])
                {
                    ri--;
                }
                else
                {
                    ci--;
                }
            }

            result.Reverse();
            return result;
        }

        /// <summary>
        /// Classifies changes between <paramref name="reference"/> and <paramref name="current"/>
        /// mod lists given the pre-computed LCS alignment. Returns a flat list of
        /// <see cref="DiffLineModel"/> entries (dependents are nested under their causal change).
        /// </summary>
        /// <remarks>
        /// Classification pipeline (Steps 0–6, matching the inline step comments in the implementation):
        /// <list type="bullet">
        ///   <item><b>Step 0</b> — Build fast lookup sets: partition reference and current items into LCS
        ///         members (unchanged), removed candidates (reference only), and new candidates (current only).</item>
        ///   <item><b>Step 1</b> — Identify shifted LCS items: LCS members whose absolute positions differ
        ///         between the two lists → candidate dependent changes (Moved).</item>
        ///   <item><b>Step 2</b> — Same-filename reconciliation: removed mod whose filename also appears in
        ///         the new set → Moved (swapped); performed before replacement detection.</item>
        ///   <item><b>Step 3</b> — Replacement detection: removed item at reference position R paired with
        ///         the new item at the corresponding LCS-aligned current position → Replaced.</item>
        ///   <item><b>Step 4</b> — Remaining new items classified as Added (beyond all surviving reference
        ///         mods) or Inserted (within the range of surviving reference mods).</item>
        ///   <item><b>Step 5</b> — Build the top-level result list: emit Moved (from Step 2), Replaced,
        ///         Removed, Added/Inserted entries in a single flat pass.</item>
        ///   <item><b>Step 6</b> — Group shifted LCS items as dependent changes under their causal
        ///         insertion/deletion and remove them from the top-level list.</item>
        /// </list>
        /// </remarks>
        internal static List<DiffLineModel> ClassifyChanges(
            IReadOnlyList<ModEntryModel> reference,
            IReadOnlyList<ModEntryModel> current,
            List<(int refIndex, int curIndex)> lcs)
        {
            // --- Step 0: Build fast lookup sets ---
            var lcsRefIndices = new HashSet<int>();
            var lcsCurIndices = new HashSet<int>();
            foreach (var (ri, ci) in lcs)
            {
                lcsRefIndices.Add(ri);
                lcsCurIndices.Add(ci);
            }

            // Items in reference not in LCS → Removed candidates
            var removedMods = new List<ModEntryModel>();
            for (int i = 0; i < reference.Count; i++)
            {
                if (!lcsRefIndices.Contains(i))
                {
                    removedMods.Add(reference[i]);
                }
            }

            // Items in current not in LCS → New candidates
            var newMods = new List<ModEntryModel>();
            for (int j = 0; j < current.Count; j++)
            {
                if (!lcsCurIndices.Contains(j))
                {
                    newMods.Add(current[j]);
                }
            }

            // --- Step 1: LCS-shifted items (same relative order, different absolute position) ---
            // These are the "moved due to neighbour insertion/deletion" entries that will become
            // dependent changes. We track them but don't emit them as top-level entries yet.
            var lcsShifted = new List<(ModEntryModel mod, int refPos, int curPos)>();
            foreach (var (ri, ci) in lcs)
            {
                // Invariant: FileService.ReadFileAsync always assigns LineNumber for every entry it
                // produces, so these values should never be null at this point.  The ?? fallbacks
                // below are purely defensive — they use the 1-based LCS index as a proxy position
                // so that shift detection can still run rather than crashing if the invariant is
                // ever violated.
                Debug.Assert(reference[ri].LineNumber.HasValue, $"reference[{ri}].LineNumber must be set — FileService.ReadFileAsync guarantees LineNumber is populated for every entry.");
                Debug.Assert(current[ci].LineNumber.HasValue,   $"current[{ci}].LineNumber must be set — FileService.ReadFileAsync guarantees LineNumber is populated for every entry.");
                int refPos = reference[ri].LineNumber ?? (ri + 1);
                int curPos = current[ci].LineNumber ?? (ci + 1);
                if (refPos != curPos)
                {
                    lcsShifted.Add((reference[ri], refPos, curPos));
                }
            }

            // --- Step 2: Same-filename reconciliation (Moved / swapped) ---
            var newByFilename = new Dictionary<string, ModEntryModel>(StringComparer.OrdinalIgnoreCase);
            foreach (var m in newMods)
            {
                newByFilename.TryAdd(m.FileName, m);
            }

            var movedResults = new List<DiffLineModel>();
            var reconciledRemovedFilenames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            // ReferenceEqualityComparer.Instance is intentional: ModEntryModel.Equals() compares by
            // FileName, so the default comparer would incorrectly de-duplicate two distinct instances
            // with the same filename. Identity comparison ensures we track the exact objects matched.
            var reconciledNewMods = new HashSet<ModEntryModel>(ReferenceEqualityComparer.Instance);

            foreach (var removed in removedMods)
            {
                if (newByFilename.TryGetValue(removed.FileName, out var matchingNew))
                {
                    // Same filename appeared in both removed and new → swapped/moved
                    string text = _localization.GetString("DiffDialog", "MovedText_Description",
                        removed.FileName,
                        removed.LineNumber ?? 0,
                        matchingNew.LineNumber ?? 0);
                    movedResults.Add(new DiffLineModel(
                        removed.FileName, text, DiffChangeType.Moved,
                        removed.LineNumber, matchingNew.LineNumber));

                    reconciledRemovedFilenames.Add(removed.FileName);
                    reconciledNewMods.Add(matchingNew);
                }
            }

            // Remove reconciled items from working sets
            var remainingRemoved = removedMods
                .Where(m => !reconciledRemovedFilenames.Contains(m.FileName))
                .ToList();
            var remainingNew = newMods
                .Where(m => !reconciledNewMods.Contains(m))
                .ToList();

            // --- Step 3: Replacement detection (LCS-aligned position matching) ---
            // Build a map from current-list positions of new items for quick lookup
            var newByCurPos = new Dictionary<int, ModEntryModel>();
            foreach (var m in remainingNew)
            {
                // Invariant: LineNumber is always populated by FileService.ReadFileAsync.
                // The ?? 0 sentinel below differs from the Step 1 fallback (which uses ri+1/ci+1
                // as a proxy position) because here we need a value that is *outside* the valid
                // line-number range so the entry is naturally excluded from position-based
                // replacement matching and falls through to be classified as Added.
                Debug.Assert(m.LineNumber.HasValue, $"remainingNew entry '{m.FileName}' must have LineNumber set — FileService.ReadFileAsync guarantees LineNumber is populated for every entry.");
                int pos = m.LineNumber ?? 0;
                // pos == 0 means LineNumber was null (no position assigned); skip — these entries
                // cannot participate in replacement matching and will be classified as Added.
                if (pos > 0 && !newByCurPos.ContainsKey(pos))
                {
                    newByCurPos[pos] = m;
                }
            }

            // For each removed mod, determine its aligned current position.
            // The expected current position of a removed mod at reference position R is:
            //   R − (number of other removed mods at reference positions < R)
            // because each earlier deletion shifts the surviving items up by one slot.

            // ReferenceEqualityComparer.Instance: same rationale as reconciledNewMods above —
            // we need object identity to avoid conflating two distinct ModEntryModel instances
            // that happen to share the same filename.
            var replacements = new Dictionary<ModEntryModel, ModEntryModel>(ReferenceEqualityComparer.Instance);
            var usedForReplacement = new HashSet<ModEntryModel>(ReferenceEqualityComparer.Instance);

            // Sort removed mods by reference position for sequential processing
            var removedByRefPos = remainingRemoved
                .OrderBy(m => m.LineNumber ?? 0)
                .ToList();

            int cumulativeDeletions = 0;
            foreach (var removed in removedByRefPos)
            {
                int refPos = removed.LineNumber ?? 0;
                // Aligned current position = reference position offset by the number of true
                // deletions that occurred before this position. Replacements do not shift
                // subsequent positions, so we only count unmatched removals.
                int alignedCurPos = refPos - cumulativeDeletions;

                if (newByCurPos.TryGetValue(alignedCurPos, out var candidate)
                    && !usedForReplacement.Contains(candidate))
                {
                    replacements[removed] = candidate;
                    usedForReplacement.Add(candidate);
                    // A replacement is not a deletion: don't shift subsequent aligned positions.
                }
                else
                {
                    // True deletion — shifts subsequent positions by one.
                    cumulativeDeletions++;
                }
            }

            // --- Step 4: Classify remaining new mods as Added or Inserted ---
            // maxSurvivingRefCurPos = highest current position of a mod that existed in reference
            // (either in LCS or as a moved/replacement pair)
            int maxSurvivingRefCurPos = 0;
            foreach (var (ri, ci) in lcs)
            {
                int curPos = current[ci].LineNumber ?? (ci + 1);
                if (curPos > maxSurvivingRefCurPos)
                    maxSurvivingRefCurPos = curPos;
            }
            // Also consider replacements: the replacement's current position counts as
            // "still within the reference range"
            foreach (var replacement in replacements.Values)
            {
                int curPos = replacement.LineNumber ?? 0;
                if (curPos > maxSurvivingRefCurPos)
                    maxSurvivingRefCurPos = curPos;
            }

            // --- Step 5: Build the top-level result list ---
            var result = new List<DiffLineModel>();

            // Replacement entries
            foreach (var removed in removedByRefPos)
            {
                if (replacements.TryGetValue(removed, out var replacement))
                {
                    int? lineNumber = removed.LineNumber ?? replacement.LineNumber;
                    string text = _localization.GetString("DiffDialog", "ReplacedText_Description",
                        removed.FileName, replacement.FileName, lineNumber ?? 0);
                    result.Add(new DiffLineModel(
                        removed.FileName, text, DiffChangeType.Replaced,
                        removed.LineNumber, replacement.LineNumber, replacement.FileName));
                }
                else
                {
                    // Truly removed
                    string text = _localization.GetString("DiffDialog", "RemovedText_Description",
                        removed.LineNumber ?? 0, removed.FileName);
                    result.Add(new DiffLineModel(
                        removed.FileName, text, DiffChangeType.Removed,
                        removed.LineNumber, null));
                }
            }

            // Moved (swapped) entries
            result.AddRange(movedResults);

            // Inserted / Added entries (new mods not used as replacements)
            foreach (var newMod in remainingNew)
            {
                if (usedForReplacement.Contains(newMod))
                    continue;

                int curPos = newMod.LineNumber ?? 0;
                bool isInserted = curPos > 0 && curPos <= maxSurvivingRefCurPos;
                DiffChangeType changeType = isInserted ? DiffChangeType.Inserted : DiffChangeType.Added;
                string actionText = isInserted
                    ? _localization.GetString("DiffDialog", "InsertedText_Action")
                    : _localization.GetString("DiffDialog", "AddedText_Action");
                string text = _localization.GetString("DiffDialog", "ModChangeText_Description",
                    curPos, newMod.FileName, actionText);
                result.Add(new DiffLineModel(
                    newMod.FileName, text, changeType,
                    null, newMod.LineNumber));
            }

            // LCS-shifted entries (dependent Moved)
            var shiftedLines = new List<DiffLineModel>();
            foreach (var (mod, refPos, curPos) in lcsShifted)
            {
                string text = _localization.GetString("DiffDialog", "MovedText_Description",
                    mod.FileName, refPos, curPos);
                shiftedLines.Add(new DiffLineModel(
                    mod.FileName, text, DiffChangeType.Moved,
                    refPos, curPos));
            }

            // --- Step 6: Group shifted (dependent) lines under their causal change ---
            if (shiftedLines.Count > 0)
            {
                // Sort shifted lines by reference position for range-boundary processing.
                // All shifted lines must have both ReferenceNumber and CurrentNumber — they are
                // constructed from LCS pairs with explicit refPos and curPos values
                // (see lcsShifted population above, Step 1 and Step 5 shiftedLines construction).
                // CurrentNumber is required by the Step 6b two-pointer sort (shiftedByCurPos).
                Debug.Assert(shiftedLines.All(s => s.ReferenceNumber.HasValue), "All shifted lines must have ReferenceNumber — they are constructed from LCS pairs with explicit refPos.");
                Debug.Assert(shiftedLines.All(s => s.CurrentNumber.HasValue), "All shifted lines must have CurrentNumber — they are constructed from LCS pairs with explicit curPos (Step 6b two-pointer sort requires a valid CurrentNumber key).");
                var shiftedByRefPos = shiftedLines
                    .OrderBy(s => s.ReferenceNumber!.Value)
                    .ToList();

                var assignedShifted = new HashSet<DiffLineModel>(ReferenceEqualityComparer.Instance);

                // --- 6a: Attribute to Removed entries using a range-based approach ---
                // Each removed mod "owns" the range from its reference position + 1 up to
                // (but not including) the next removal's reference position.  This mirrors
                // the original DetectAndAssignDependentChanges logic and ensures that
                // multiple consecutive removals each receive the correct subset of dependents.
                var removedEntries = result
                    .Where(r => r.ChangeType == DiffChangeType.Removed && r.ReferenceNumber.HasValue)
                    .OrderBy(r => r.ReferenceNumber!.Value)
                    .ToList();

                for (int idx = 0; idx < removedEntries.Count; idx++)
                {
                    var removed = removedEntries[idx];
                    int startPos = removed.ReferenceNumber!.Value + 1;

                    // Upper bound: the reference position of the next removal (exclusive).
                    int? stopBefore = (idx + 1 < removedEntries.Count)
                        ? removedEntries[idx + 1].ReferenceNumber
                        : (int?)null;

                    foreach (var shifted in shiftedByRefPos)
                    {
                        if (assignedShifted.Contains(shifted))
                            continue;

                        int refPos = shifted.ReferenceNumber!.Value;
                        if (refPos < startPos)
                            continue;
                        if (stopBefore.HasValue && refPos >= stopBefore.Value)
                            break;

                        removed.DependentChanges.Add(shifted);
                        assignedShifted.Add(shifted);
                    }

                    // Set cause properties and format summary if this removed entry received dependent changes
                    if (removed.HasDependentChanges)
                    {
                        removed.DependentChangeCauseFileName = removed.FileName;
                        removed.DependentChangeCauseAction = "DependentCause_Removed";
                        string causeText = _localization.GetString("DiffDialog", "DependentCause_Removed");
                        string summaryKey = removed.DependentChanges.Count == 1
                            ? "DependentChangesSummary_Singular"
                            : "DependentChangesSummary";
                        removed.DependentChangesSummary = _localization.GetString(
                            "DiffDialog", summaryKey,
                            removed.DependentChanges.Count, removed.FileName, causeText);
                    }
                }

                // --- 6b: Attribute remaining unassigned shifted mods to Inserted entries ---
                // An inserted mod at current position P shifts all surviving reference mods
                // at current positions >= P.
                //
                // Two-pointer approach: pre-sort shifted mods by CurrentNumber so the "find
                // first affected" lookup can advance a single index rather than re-scanning
                // the full list for every insertion.  insertedEntries is already sorted by
                // CurrentNumber (ascending), so shiftedIdx never needs to retreat — amortized
                // O(k) for the firstAffected lookups across all insertions.
                var insertedEntries = result
                    .Where(r => r.ChangeType == DiffChangeType.Inserted && r.CurrentNumber.HasValue)
                    .OrderBy(r => r.CurrentNumber!.Value)
                    .ToList();

                // shiftedByCurPos drives the two-pointer firstAffected lookup (sorted by CurrentNumber).
                // shiftedByRefPos is still used for the range-attribution inner loop (sorted by ReferenceNumber).
                var shiftedByCurPos = shiftedByRefPos
                    .OrderBy(s => s.CurrentNumber ?? int.MaxValue)
                    .ToList();

                int shiftedIdx = 0;
                foreach (var inserted in insertedEntries)
                {
                    int insertedCurPos = inserted.CurrentNumber!.Value;

                    // Advance past entries that cannot be attributed to this insertion.
                    // Two logically distinct reasons combine in the || clause:
                    //   (1) Already attributed — entry was assigned to a prior Removed (Step 6a)
                    //       or to an earlier Inserted (a prior iteration of this loop).
                    //   (2) Precedes this insertion — entry's CurrentNumber is below the current
                    //       insertion position, so it was not shifted by this particular insertion.
                    // Because insertedEntries is sorted ascending by CurrentNumber, shiftedIdx
                    // only ever moves forward — the two-pointer amortisation guarantee.
                    while (shiftedIdx < shiftedByCurPos.Count &&
                           (assignedShifted.Contains(shiftedByCurPos[shiftedIdx]) ||  // (1) already attributed
                            shiftedByCurPos[shiftedIdx].CurrentNumber < insertedCurPos)) // (2) precedes insertion
                    {
                        shiftedIdx++;
                    }

                    if (shiftedIdx >= shiftedByCurPos.Count)
                        break;

                    var firstAffected = shiftedByCurPos[shiftedIdx];
                    int startRefPos = firstAffected.ReferenceNumber!.Value;

                    // Next removal in reference space (to serve as upper boundary).
                    // O(r) per insertion — acceptable for typical mod-list sizes.
                    var nextRemoved = removedEntries
                        .Where(r => r.ReferenceNumber >= startRefPos)
                        .OrderBy(r => r.ReferenceNumber)
                        .FirstOrDefault();

                    int? stopBefore = nextRemoved?.ReferenceNumber;

                    foreach (var shifted in shiftedByRefPos)
                    {
                        if (assignedShifted.Contains(shifted))
                            continue;

                        int refPos = shifted.ReferenceNumber!.Value;
                        if (refPos < startRefPos)
                            continue;
                        if (stopBefore.HasValue && refPos >= stopBefore.Value)
                            break;

                        inserted.DependentChanges.Add(shifted);
                        assignedShifted.Add(shifted);
                    }

                    // Set cause properties and format summary if this inserted entry received dependent changes
                    if (inserted.HasDependentChanges)
                    {
                        inserted.DependentChangeCauseFileName = inserted.FileName;
                        inserted.DependentChangeCauseAction = "DependentCause_Inserted";
                        string causeText = _localization.GetString("DiffDialog", "DependentCause_Inserted");
                        string summaryKey = inserted.DependentChanges.Count == 1
                            ? "DependentChangesSummary_Singular"
                            : "DependentChangesSummary";
                        inserted.DependentChangesSummary = _localization.GetString(
                            "DiffDialog", summaryKey,
                            inserted.DependentChanges.Count, inserted.FileName, causeText);
                    }
                }

                // Fallback: any parent entry that has dependent changes but no cause attribution
                // (should not occur under current classification logic, but ensures display is never empty)
                foreach (var entry in result)
                {
                    if (entry.HasDependentChanges && string.IsNullOrEmpty(entry.DependentChangesSummary))
                    {
                        Debug.Assert(false,
                            $"DependentChangesSummary was not set for '{entry.FileName}' (ChangeType={entry.ChangeType}, " +
                            $"DependentChanges.Count={entry.DependentChanges.Count}). " +
                            "Step 6a/6b attribution logic should always populate the summary for entries with dependent changes.");
                        entry.DependentChangesSummary = _localization.GetString(
                            "DiffDialog", "DependentChangesSummary_Generic",
                            entry.DependentChanges.Count);
                    }
                }

                // Add any unassigned shifted mods as top-level Moved entries
                foreach (var shifted in shiftedLines)
                {
                    if (!assignedShifted.Contains(shifted))
                    {
                        result.Add(shifted);
                    }
                }
            }

            // --- Context lines: include Unchanged items for LCS entries with identical absolute positions ---
            foreach (var (ri, ci) in lcs)
            {
                var refMod = reference[ri];
                var curMod = current[ci];
                // Only include as context if this item was NOT shifted (i.e., same position in both lists)
                // and not already a top-level changed item.
                int refPos = refMod.LineNumber ?? (ri + 1);
                int curPos = curMod.LineNumber ?? (ci + 1);
                if (refPos == curPos)
                {
                    result.Add(new DiffLineModel(
                        refMod.FileName, refMod.FileName, DiffChangeType.Unchanged,
                        refPos, curPos));
                }
            }

            // Sort the top-level result in reference order (matching the old implementation's
            // ordering which iterated diffs sorted by ReferenceNumber then CurrentNumber).
            // int.MaxValue sentinel: entries with no ReferenceNumber (pure Added mods) sort
            // after all reference-positioned entries, placing new mods at the end of the list.
            result.Sort((x, y) =>
            {
                int xKey = x.ReferenceNumber ?? int.MaxValue;
                int yKey = y.ReferenceNumber ?? int.MaxValue;
                if (xKey != yKey) return xKey.CompareTo(yKey);
                int xCur = x.CurrentNumber ?? int.MaxValue;
                int yCur = y.CurrentNumber ?? int.MaxValue;
                return xCur.CompareTo(yCur);
            });

            return TrimToContextWindow(result);
        }

        /// <summary>
        /// Trims the diff output to show only changed items and their immediate neighbors.
        /// Retains one <paramref name="contextSize"/> Unchanged item above and below each
        /// changed item (or contiguous group of changed items). Inserts a <see cref="DiffChangeType.Separator"/>
        /// item between non-adjacent context groups to provide visual continuity.
        /// </summary>
        private static List<DiffLineModel> TrimToContextWindow(List<DiffLineModel> items, int contextSize = 1)
        {
            if (items.Count == 0)
            {
                return items;
            }

            // Build a boolean "keep" array: an item is kept if it is a non-Unchanged/non-Separator
            // change, or if it is within contextSize positions of such an item.
            var keep = new bool[items.Count];

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].ChangeType != DiffChangeType.Unchanged && items[i].ChangeType != DiffChangeType.Separator)
                {
                    keep[i] = true;
                    // Mark neighbors within contextSize
                    for (int k = 1; k <= contextSize; k++)
                    {
                        if (i - k >= 0) keep[i - k] = true;
                        if (i + k < items.Count) keep[i + k] = true;
                    }
                }
            }

            // Build the trimmed output, inserting Separator between non-adjacent kept groups
            var trimmed = new List<DiffLineModel>(items.Count);
            bool hadPreviousKept = false;
            int lastKeptIndex = -1;

            for (int i = 0; i < items.Count; i++)
            {
                if (!keep[i])
                {
                    continue;
                }

                // If there's a gap since the last kept item, insert a separator
                if (hadPreviousKept && i > lastKeptIndex + 1)
                {
                    trimmed.Add(new DiffLineModel(
                        string.Empty, "···", DiffChangeType.Separator,
                        null, null));
                }

                trimmed.Add(items[i]);
                hadPreviousKept = true;
                lastKeptIndex = i;
            }

            return trimmed;
        }

    }
}
