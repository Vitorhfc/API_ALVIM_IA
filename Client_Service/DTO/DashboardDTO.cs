namespace Client_Service.DTO
{
    public class DashboardData
    {
        public List<MetricCard> Metrics { get; set; } = new();
        public SentimentosData SentimentosData { get; set; } = new();
        public List<VolumeMensagemData> VolumeMensagensData { get; set; } = new();
        public List<CategoriaData> CategoriasData { get; set; } = new();
    }

    public class MetricCard
    {
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Change { get; set; } = string.Empty;
        public bool ChangePositive { get; set; }
        public string Icon { get; set; } = string.Empty;
        public string IconColor { get; set; } = string.Empty;
    }

    public class SentimentosData
    {
        public int Positivo { get; set; }
        public int Neutro { get; set; }
        public int Negativo { get; set; }
    }

    public class VolumeMensagemData
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    public class CategoriaData
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}
