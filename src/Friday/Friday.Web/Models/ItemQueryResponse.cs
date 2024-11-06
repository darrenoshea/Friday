using System.Text.Json;
using System.Text.Json.Serialization;

namespace Friday.Web.Models;

public class ItemQueryRequest
{
    public ItemQueryRequest(string query)
    {
        Query = query;
    }

    [JsonPropertyName("query")]
    public string Query { get; set; }
}

public record Week
{
    public Week(Item item)
    {
        var date = item.column_values.FirstOrDefault(x => x.id == "date__1")?.value_T<ReportedDate>();

        if (date != null)
        {
            var reportedDate = date.date_DateOnly.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.Zero));
            var thisFriday = DateOnly.FromDateTime(reportedDate.AddDays(DayOfWeek.Friday - reportedDate.DayOfWeek));
            Ending = thisFriday;
        }
    }

    public DateOnly Ending { get; init; }
    public decimal TotalHours { get; set; } = 0;

    public string Id => $"week{Ending.ToString("yyyyMMdd")}";

    public override string ToString()
    {
        return $"Week ending {Ending.ToString("O")}";
    }
}

public abstract class TypeOfWeek
{
    public string Label { get; set; } = null!;

    public abstract Task UpdateMonday();
}

public class Work : TypeOfWeek
{
    public Work()
    {
        Label = "Work";
    }

    public override Task UpdateMonday()
    {
        return Task.Delay(5000);
    }
}

public class Vacation : TypeOfWeek
{
    public Vacation()
    {
        Label = "Vacation";
    }

    public override Task UpdateMonday()
    {
        return Task.Delay(5000);
    }
}

public class ItemQueryBoard
{
    public ItemsPage items_page { get; set; }
}

public class ColumnValue
{
    public string id { get; set; }
    public string value { get; set; }

    public decimal value_decimal => decimal.Parse(value_string);
    public string? value_string => JsonSerializer.Deserialize<string>(value);

    public T value_T<T>()
    {
        return JsonSerializer.Deserialize<T>(value);
    }
}

public class ItemQueryData
{
    public List<ItemQueryBoard> boards { get; set; }
}

public class Item
{
    public string id { get; set; }
    public string name { get; set; }
    public List<ColumnValue> column_values { get; set; }
}

public class ItemsPage
{
    public string cursor { get; set; }
    public List<Item> items { get; set; }
}

public class ItemQueryResponse
{
    public ItemQueryData data { get; set; }
    public int account_id { get; set; }
}

public class ReportedDate
{
    const string Format = "yyyy-MM-dd";

    public string date { get; set; }
    public DateTime changed_at { get; set; }

    public DateOnly date_DateOnly => DateOnly.ParseExact(date, Format);
}