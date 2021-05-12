namespace Performance
{
    public class PeformanceEntity{
        public Type type { get; set; }
        public string connector { get; set; } 
        public string device { get; set; }

        public string process { get; set;}

        public string usage {get; set; }

    }

    public enum Type{
        cpuusage,
        memoryusage,
        unknownprocess
    }
}