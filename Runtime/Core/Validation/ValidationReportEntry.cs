namespace FTFoundation.Core.Validation
{
    public record ValidationReportEntry
    {
        public string name;
        public ValidationStatus validationStatus;
        public string message;
    }
}
