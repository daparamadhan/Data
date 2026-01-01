using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Datas
{
    public class Sampah
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Nama { get; set; }
        public int Jumlah { get; set; }
        public string Lokasi { get; set; }
    }
}
