namespace SEODesk.Application.Common;

/// <summary>
/// Базовий результат операції з можливістю помилки
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }

    private Result(bool isSuccess, T? value, string? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);
}

/// <summary>
/// DTO для метрик
/// </summary>
public class MetricDto
{
    public long Clicks { get; set; }
    public long Impressions { get; set; }
    public double Ctr { get; set; }
    public double AvgPosition { get; set; }
    public int KeywordsCount { get; set; }
}

/// <summary>
/// DTO для time series точки
/// </summary>
public class TimeSeriesPointDto
{
    public string Date { get; set; } = string.Empty;
    public long Clicks { get; set; }
    public long Impressions { get; set; }
    public double Ctr { get; set; }
    public double AvgPosition { get; set; }
    public int KeywordsCount { get; set; }
}
