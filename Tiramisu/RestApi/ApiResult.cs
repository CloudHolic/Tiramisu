namespace Tiramisu.RestApi
{
    public class ApiResult<T>
    {
        public bool Success { get; set; }

        public ApiError Error { get; set; }

        public T Response { get; set; }
    }
}