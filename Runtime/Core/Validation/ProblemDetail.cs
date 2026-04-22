namespace FTFoundation.Core.Validation
{
    public record ProblemDetail
    {
        public ProblemDetailType ProblemDetailType { get; }
        public string Message { get; }

        public ProblemDetail(ProblemDetailType problemDetailType, string message)
        {
            ProblemDetailType = problemDetailType;
            Message = message;
        }
    }
}