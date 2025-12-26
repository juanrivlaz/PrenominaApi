namespace PrenominaApi.Models.Dto
{
    public class Paginator
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public string? Search {  get; set; }
        public bool? NoPagination { get; set; }
    }
}
