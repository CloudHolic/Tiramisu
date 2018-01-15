namespace Tiramisu.RestApi
{
    public class ApiError
    {
        public int Status { get; set; }

        public string Message { get; set; }

        public override string ToString()
        {
            return Message;
        }
    }
}