namespace FastGithub.FlowAnalyze
{
    public record FlowRate
    {
        /// <summary>
        /// 获取总读上行
        /// </summary>
        public long TotalRead { get; init; }

        /// <summary>
        /// 获取总下行
        /// </summary>
        public long TotalWrite { get; init; }

        public double ReadRate { get; init; }

        public double WriteRate { get; init; }
    }
}
