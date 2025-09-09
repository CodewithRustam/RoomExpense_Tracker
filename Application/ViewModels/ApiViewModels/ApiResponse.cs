namespace Services.ViewModels.ApiViewModels
{
    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public ApiResponse(bool success, string message)
        {
            Success = success;
            Message = message;
        }
        public static ApiResponse Ok(string message = "Success") => new(true, message);

        public static ApiResponse Fail(string message) => new(false, message);
    }
    public class ApiResponse<T> : ApiResponse
    {
        public T? Data { get; set; }
        public ApiResponse(bool success, string message, T? data) : base(success, message)
        {
            Data = data;
        }

        public static ApiResponse<T> Ok(T? data, string message = "Success")
            => new(true, message, data);

        public static ApiResponse<T> Fail(T? data, string message)
            => new(false, message,data);
    }
}
