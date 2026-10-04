namespace Racing.Domain.Competitions;

public interface IScoringPolicy
{
    int CalculatePoints(int rank, bool isDnf);
}

public sealed class StandardScoringPolicy : IScoringPolicy
{
    private static readonly int[] PointsByRank = [10, 8, 6, 5, 4, 3, 2, 1];

    public int CalculatePoints(int rank, bool isDnf)
    {
        if (isDnf || rank < 1) return 0;
        int idx = rank - 1;
        return idx < PointsByRank.Length ? PointsByRank[idx] : 0;
    }
}
