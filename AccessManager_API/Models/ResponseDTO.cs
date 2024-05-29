namespace MyHub.Models
{
    //17-05-2024 by Periya Samy P CHC1761
    public class ResponseDTO
    {
        public bool Status { get; set; } = true;
        public string ErrorCode { get; set; } = string.Empty;
        public object Data { get; set; } 

        public ResponseDTO() { }

        public ResponseDTO(object data) 
        {
            Data = data;
        } 

        public ResponseDTO(bool status, string errorCode)
        {
            Status = status;
            ErrorCode = errorCode;
        }
    }
}
