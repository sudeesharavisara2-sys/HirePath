using HirePathAI.Application.Interfaces;

namespace HirePathAI.Infrastructure.Repositories;

public sealed class InMemoryAtsAnalysisResultStore : IAtsAnalysisResultStore
{
    private sealed record Entry(string Id, AtsAnalysisResult Result, DateTimeOffset CreatedUtc);

    private readonly object _gate = new();
    private readonly List<Entry> _entries = new();
    private readonly TimeSpan _ttl;
    private readonly int _capacity;

    public InMemoryAtsAnalysisResultStore(int capacity = 200, TimeSpan? ttl = null)
    {
        _capacity = Math.Max(50, capacity);
        _ttl = ttl ?? TimeSpan.FromMinutes(10);
    }

    public string Put(AtsAnalysisResult result)
    {
        var id = Guid.NewGuid().ToString("N");
        var now = DateTimeOffset.UtcNow;

        lock (_gate)
        {
            Prune(now);
            _entries.Add(new Entry(id, result, now));

            if (_entries.Count > _capacity)
            {
                _entries.RemoveRange(0, _entries.Count - _capacity);
            }
        }

        return id;
    }

    public bool TryTake(string id, out AtsAnalysisResult result)
    {
        var now = DateTimeOffset.UtcNow;
        lock (_gate)
        {
            Prune(now);
            var idx = _entries.FindIndex(e => string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase));
            if (idx < 0)
            {
                result = default!;
                return false;
            }

            result = _entries[idx].Result;
            _entries.RemoveAt(idx);
            return true;
        }
    }

    private void Prune(DateTimeOffset now)
    {
        if (_entries.Count == 0) return;
        _entries.RemoveAll(e => now - e.CreatedUtc > _ttl);
    }
}

