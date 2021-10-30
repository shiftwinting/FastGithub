namespace FastGithub.FlowAnalyze
{
    public record FlowRate
    {
        public double ReadRate { get; init; }

        public double WriteRate { get; init; }
    }
}
