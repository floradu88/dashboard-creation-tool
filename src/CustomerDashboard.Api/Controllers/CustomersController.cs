using System.ComponentModel.DataAnnotations;
using CustomerDashboard.Application;
using CustomerDashboard.Domain;
using Microsoft.AspNetCore.Mvc;

namespace CustomerDashboard.Api.Controllers;

[ApiController]
[Route("api/v1/customers")]
public sealed class CustomersController(GetCustomersHandler customers, GetOverviewHandler overview, GetCommercialHandler commercial,
    GetUsageHandler usage, GetSlaHandler slas, GetWorkHandler work, GetRequestsHandler requests, GetNewsHandler news,
    GetMarketHandler market, GetAccountHandler account, GetMetricsHandler metrics, GetSourcesHandler sources) : ControllerBase
{
    [HttpGet]
    public ActionResult<DashboardResponse<CustomerListData>> List([FromQuery, StringLength(100)] string? search,
        [FromQuery, Range(1, 100000)] int page = 1, [FromQuery, Range(1, 100)] int pageSize = 25) =>
        Ok(customers.Handle(search, page, pageSize));

    [HttpGet("{id}/overview")]
    public ActionResult<DashboardResponse<OverviewData>> Overview(string id, [FromQuery, Required] DateOnly? from, [FromQuery, Required] DateOnly? to) =>
        Read(id, from, to, overview.Handle);

    [HttpGet("{id}/commercial")]
    public ActionResult<DashboardResponse<CommercialData>> Commercial(string id, [FromQuery, Required] DateOnly? from, [FromQuery, Required] DateOnly? to) =>
        Read(id, from, to, commercial.Handle);

    [HttpGet("{id}/usage")]
    public ActionResult<DashboardResponse<UsageData>> Usage(string id, [FromQuery, Required] DateOnly? from, [FromQuery, Required] DateOnly? to) =>
        Read(id, from, to, usage.Handle);

    [HttpGet("{id}/slas")]
    public ActionResult<DashboardResponse<SlaData>> Slas(string id, [FromQuery, Required] DateOnly? from, [FromQuery, Required] DateOnly? to) =>
        Read(id, from, to, slas.Handle);

    [HttpGet("{id}/work-items")]
    public ActionResult<DashboardResponse<WorkData>> Work(string id, [FromQuery, Required] DateOnly? from, [FromQuery, Required] DateOnly? to,
        [FromQuery, Range(1, 100000)] int page = 1, [FromQuery, Range(1, 100)] int pageSize = 25, [FromQuery] string sort = "updatedDesc") =>
        Sort(sort, GetWorkHandler.Sorts) ?? Read(id, from, to, (customer, period) => work.Handle(customer, period, page, pageSize, sort));

    [HttpGet("{id}/requests")]
    public ActionResult<DashboardResponse<RequestData>> Requests(string id, [FromQuery, Required] DateOnly? from, [FromQuery, Required] DateOnly? to,
        [FromQuery, Range(1, 100000)] int page = 1, [FromQuery, Range(1, 100)] int pageSize = 25, [FromQuery] string sort = "receivedDesc") =>
        Sort(sort, GetRequestsHandler.Sorts) ?? Read(id, from, to, (customer, period) => requests.Handle(customer, period, page, pageSize, sort));

    [HttpGet("{id}/news")]
    public ActionResult<DashboardResponse<NewsData>> News(string id, [FromQuery, Required] DateOnly? from, [FromQuery, Required] DateOnly? to,
        [FromQuery, Range(1, 100000)] int page = 1, [FromQuery, Range(1, 100)] int pageSize = 25, [FromQuery] string sort = "publishedDesc", [FromQuery, StringLength(40)] string? topic = null) =>
        Sort(sort, GetNewsHandler.Sorts) ?? Read(id, from, to, (customer, period) => news.Handle(customer, period, page, pageSize, sort, topic));

    [HttpGet("{id}/market-data")]
    public ActionResult<DashboardResponse<MarketData>> Market(string id, [FromQuery, Required] DateOnly? from, [FromQuery, Required] DateOnly? to) =>
        Read(id, from, to, market.Handle);

    [HttpGet("{id}/account")]
    public ActionResult<DashboardResponse<AccountData>> Account(string id)
    {
        var result = account.Handle(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{id}/metrics")]
    public ActionResult<DashboardResponse<MetricsData>> Metrics(string id, [FromQuery, Required] DateOnly? from, [FromQuery, Required] DateOnly? to,
        [FromQuery] DateOnly? compareFrom, [FromQuery] DateOnly? compareTo)
    {
        if (compareFrom.HasValue != compareTo.HasValue) return Problem(statusCode: 400, title: "compareFrom and compareTo must be supplied together.");
        ReportingPeriod period;
        ReportingPeriod comparison;
        try
        {
            period = new ReportingPeriod(from!.Value, to!.Value);
            comparison = compareFrom is DateOnly start && compareTo is DateOnly end ? new ReportingPeriod(start, end) : period.Previous();
        }
        catch (ArgumentException error) { return Problem(statusCode: 400, title: error.Message); }
        var result = metrics.Handle(id, period, comparison);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{id}/sources")]
    public ActionResult<DashboardResponse<SourceListData>> Sources(string id)
    {
        var result = sources.Handle(id);
        return result is null ? NotFound() : Ok(result);
    }

    private ActionResult? Sort(string sort, IReadOnlyList<string> allowed) =>
        allowed.Contains(sort) ? null : Problem(statusCode: 400, title: "That sort option is not supported.");

    private ActionResult Read<T>(string id, DateOnly? from, DateOnly? to, Func<string, ReportingPeriod, DashboardResponse<T>?> handle)
    {
        ReportingPeriod period;
        try { period = new ReportingPeriod(from!.Value, to!.Value); }
        catch (ArgumentException error) { return Problem(statusCode: 400, title: error.Message); }
        var result = handle(id, period);
        return result is null ? NotFound() : Ok(result);
    }
}
