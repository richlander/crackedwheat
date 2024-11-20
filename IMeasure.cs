namespace CrackedWheat;

public interface IMeasure
{
    string Kind { get; }
    int Index { get; }
    int Score { get; }
}