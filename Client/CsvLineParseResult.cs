using Common;

namespace Client
{
    public class CsvLineParseResult
    {
        public bool Success { get; set; }

        public DroneSample Sample { get; set; }

        public string ErrorMessage { get; set; }

        public int LineNumber { get; set; }

        public string RawLine { get; set; }
    }
}