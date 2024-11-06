using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Friday.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Friday.Web.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
        ReportedWeeks = new List<Week>();
        WeekTypes = new List<TypeOfWeek> { new Vacation() };
    }

    [FromQuery(Name = "boardId")]
    public string? BoardId { get; set; }

    [BindProperty]
    [Required]
    public string? SelectedWeek { get; set; }

    public IList<Week> ReportedWeeks { get; set; }

    public IList<SelectListItem> MissingWeeks { get; set; } =
        new List<SelectListItem> { new("Select week ending date", null) };

    [BindProperty]
    public string? SelectedWeekType { get; set; }

    public IList<TypeOfWeek> WeekTypes { get; set; }

    public async Task OnGet()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        if (BoardId == null)
        {
            BoardId = "6876965797";
        }

        //await GetBoards();
        await GetWeeks(BoardId);
    }

    public void OnPost()
    {
        if (!ModelState.IsValid)
            return;
    }
    private async Task GetBoards()
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.monday.com/v2")
        };

        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {User.Claims.First(c => c.Type == "token").Value}");
        var queryJson = """
                          query {
                            boards (limit: 500) {
                              id
                              name
                            }
                          }
                        """;

        var queryObject = new ItemQueryRequest(queryJson);
        var response = await httpClient.PostAsJsonAsync("", queryObject);
        var responseData = await response.Content.ReadAsStringAsync();
    }

    private async Task GetWeeks(string boardId)
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.monday.com/v2")
        };

        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {User.Claims.First(c => c.Type == "token").Value}");
        var queryJson = """
                     query {
                       boards (ids: __BOARD_ID__) {
                         items_page (limit: 500, query_params: {rules: [{column_id: "people0", compare_value: ["assigned_to_me"]}], operator: and}) {
                           cursor
                           items {
                             id
                             name
                             column_values {
                                id
                                value
                             }
                           }
                         }
                       }
                     }
                   """.Replace("__BOARD_ID__", boardId);

        var queryObject = new ItemQueryRequest(queryJson);
        var response = await httpClient.PostAsJsonAsync("", queryObject);
        var responseData = await response.Content.ReadAsStringAsync();
        if (responseData.IndexOf("missingRequiredPermissions") >= 0)
        {
            Response.Redirect("/Error");
            return;
        }

        var responseObject = JsonSerializer.Deserialize<ItemQueryResponse>(responseData);
        var dictionary = new Dictionary<DateOnly, Week>();
        foreach (var item in responseObject.data.boards[0].items_page.items)
        {
            var week = new Week(item);
            if (!dictionary.ContainsKey(week.Ending))
            {
                dictionary.Add(week.Ending, week);
            }

            dictionary[week.Ending].TotalHours += item.column_values.FirstOrDefault(x => x.id == "labor_budget_spent")?.value_decimal ?? 0;
        }

        foreach (var week in dictionary.Values.ToList().OrderByDescending(x => x.Ending))
        {
            ReportedWeeks.Add(week);
        }

        var thisFriday = DateOnly.FromDateTime(DateTime.Today.AddDays(DayOfWeek.Friday - DateTime.Today.DayOfWeek));
        var minWeek = ReportedWeeks.Last().Ending;
        var allWeeks = new List<DateOnly>();
        while (thisFriday > minWeek)
        {
            allWeeks.Add(thisFriday);
            thisFriday = thisFriday.AddDays(-7);
        }

        foreach (var week in allWeeks)
        {
            if (dictionary.ContainsKey(week))
                continue;

            MissingWeeks.Add(new SelectListItem($"Week ending {week}", week.ToString()));
        }
    }
}
