using System;

namespace Datas.Models
{
    [Serializable]
    public class Sampah
    {
        public string Id { get; set; }

        public string Kabupaten { get; set; }
        public string Jenis { get; set; }
        public double Berat { get; set; }
        public DateTime Tanggal { get; set; }
    }
}
