using System.Collections.Generic;

// Everything the player has read, used by the backlog and by rollback.
// Rollback is read-only: choices stay locked, so it only moves a cursor through this list and never changes story state.
public class DialogueHistory
{
    public const int MaxEntries = 300;
    public const int MaxSavedEntries = 100;

    private readonly List<HistoryEntry> entries = new List<HistoryEntry>();
    private int currentSession = -1;
    private int nextSession;
    private int cursor = -1; // -1 = showing the present

    public IReadOnlyList<HistoryEntry> Entries => entries;
    public int CurrentSession => currentSession;
    public bool IsRollingBack => cursor >= 0;
    public int ViewIndex => IsRollingBack ? cursor : entries.Count - 1;

    // The entry currently on screen in live mode. Null outside a dialogue session.
    public HistoryEntry Present
    {
        get
        {
            if (currentSession < 0 || entries.Count == 0)
                return null;

            HistoryEntry last = entries[entries.Count - 1];
            return last.session == currentSession ? last : null;
        }
    }

    public void BeginSession()
    {
        currentSession = nextSession++;
        cursor = -1;
    }

    public void EndSession()
    {
        currentSession = -1;
        cursor = -1;
    }

    public HistoryEntry Append(HistoryEntry entry)
    {
        cursor = -1; // New content always brings the view back to the present
        entry.session = currentSession;
        entries.Add(entry);

        if (entries.Count > MaxEntries)
            entries.RemoveRange(0, entries.Count - MaxEntries);

        return entry;
    }

    public int SessionStartIndex()
    {
        int i = entries.Count;

        while (i > 0 && entries[i - 1].session == currentSession)
            i--;

        return i;
    }

    public bool StepBack()
    {
        if (Present == null)
            return false;

        int from = IsRollingBack ? cursor : entries.Count - 1;
        int target = from - 1;

        if (target < SessionStartIndex())
            return false;

        cursor = target;
        return true;
    }

    // Moves toward the present. Never advances the live dialogue.
    public bool StepForward()
    {
        if (!IsRollingBack)
            return false;

        cursor++;

        if (cursor >= entries.Count - 1)
            cursor = -1;

        return true;
    }

    public void ExitRollback() => cursor = -1;

    public List<HistoryEntry> Snapshot()
    {
        int start = entries.Count > MaxSavedEntries ? entries.Count - MaxSavedEntries : 0;
        return entries.GetRange(start, entries.Count - start);
    }

    public void Load(List<HistoryEntry> savedEntries)
    {
        entries.Clear();

        if (savedEntries != null)
        {
            foreach (HistoryEntry entry in savedEntries)
            {
                if (entry != null)
                    entries.Add(entry);
            }
        }

        nextSession = 0;
        foreach (HistoryEntry entry in entries)
        {
            if (entry.session >= nextSession)
                nextSession = entry.session + 1;
        }

        currentSession = -1;
        cursor = -1;
    }

    // Continues a session restored from a save, so rollback can still reach lines read before saving
    public void ResumeSession(int session)
    {
        if (session < 0)
        {
            BeginSession();
            return;
        }

        currentSession = session;
        cursor = -1;

        if (nextSession <= session)
            nextSession = session + 1;
    }

    public void Clear()
    {
        entries.Clear();
        currentSession = -1;
        nextSession = 0;
        cursor = -1;
    }
}
