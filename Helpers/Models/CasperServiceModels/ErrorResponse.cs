namespace Helpers.Models.CasperServiceModels
{
    public class ErrorResponse
    {
        public ErrorResult error { get; set; }
    }

    public class ErrorResult
    {
        public string code { get; set; }
        public string description { get; set; }
        public string message { get; set; }
    }
}