namespace TestBinanceAPI.Models
{
    public class BinanceResponce
    {
        public string e { get; set; }
        public long E { get; set; }
        public string s { get; set; }
        public int a { get; set; }
        public string p { get; set; }
        public string q { get; set; }
        public int f { get; set; }
        public int l { get; set; }
        public long T { get; set; }
        public bool m { get; set; }
        public bool M { get; set; }

        public override string ToString()
        {
            return $"{s} Price: {p} Quantity: {q}";
        }
    }
}
