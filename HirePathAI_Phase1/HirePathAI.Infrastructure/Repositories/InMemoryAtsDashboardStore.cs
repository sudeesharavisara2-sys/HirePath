using HirePathAI.Application.Interfaces;

namespace HirePathAI.Infrastructure.Repositories;

public sealed class InMemoryAtsDashboardStore : IAtsDashboardStore
{
    private readonly object _gate = new();
    private readonly List<AtsCandidateSnapshot> _items = new();
    private readonly int _capacity;

    public InMemoryAtsDashboardStore(int capacity = 200)
    {
        _capacity = Math.Max(50, capacity);
    }

    public void Add(AtsCandidateSnapshot snapshot)
    {
        lock (_gate)
        {
            _items.Add(snapshot);
            if (_items.Count <= _capacity)
            {
                return;
            }

            var overflow = _items.Count - _capacity;
            _items.RemoveRange(0, overflow);
        }
    }

    public AtsDashboardSummary GetSummary(int takeLatest = 20)
    {
        lock (_gate)
        {
            var total = _items.Count;
            if (total == 0)
            {
                return new AtsDashboardSummary(
                    TotalProcessed: 0,
                    AverageOverallScore: 0,
                    SelectedPercent: 0,
                    AverageMlConfidencePercent: 0,
                    SelectedCount: 0,
                    ConsiderCount: 0,
                    RejectedCount: 0,
                    LatestCandidates: Array.Empty<AtsCandidateSnapshot>(),
                    TopCandidates: Array.Empty<AtsCandidateSnapshot>());
            }

            var avgOverall = _items.Average(x => x.OverallScore);
            var avgConfidencePct = _items.Average(x => x.MlConfidence) * 100.0;
            
            var selectedCount = _items.Count(x => string.Equals(x.Decision, "Selected", StringComparison.OrdinalIgnoreCase) || string.Equals(x.Decision, "Select", StringComparison.OrdinalIgnoreCase));
            var considerCount = _items.Count(x => string.Equals(x.Decision, "Consider", StringComparison.OrdinalIgnoreCase));
            var rejectedCount = _items.Count(x => string.Equals(x.Decision, "Rejected", StringComparison.OrdinalIgnoreCase) || string.Equals(x.Decision, "Reject", StringComparison.OrdinalIgnoreCase));
            var selectedPct = (selectedCount * 100.0) / total;

            var latest = _items
                .OrderByDescending(x => x.Timestamp)
                .Take(Math.Clamp(takeLatest, 1, 100))
                .ToArray();

            var top = _items
                .OrderByDescending(x => x.OverallScore)
                .ThenByDescending(x => x.MlConfidence)
                .Take(10)
                .ToArray();

            return new AtsDashboardSummary(
                TotalProcessed: total,
                AverageOverallScore: avgOverall,
                SelectedPercent: selectedPct,
                AverageMlConfidencePercent: avgConfidencePct,
                SelectedCount: selectedCount,
                ConsiderCount: considerCount,
                RejectedCount: rejectedCount,
                LatestCandidates: latest,
                TopCandidates: top);
        }
    }
}

