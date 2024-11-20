namespace CrackedWheat;

public class Score
{
    private int _score;
    private int _max;

    public void Add(OpenRatioMeasure measure)
    {
        _score += measure.Score;
        _max += 10;
    }

    public void Add(params ReadOnlySpan<OpenRatioMeasure> measures)
    {
        foreach(var measure in measures)
        {
            Add(measure);
        }
    }

    public void Add(TimestampMeasure measure)
    {
        _score += measure.Score;
        _max += 10;
    }

    public void Add(params List<TimestampMeasure> measures)
    {
        foreach(var measure in measures)
        {
            Add(measure);
        }
    }

    public int Get() => (int)(_score / (double)_max * 100);
}