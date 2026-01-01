using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Datas.Models;

namespace Datas.Services
{
    public class MongoService
    {
        private readonly IMongoCollection<Sampah> _collection;

        public MongoService(string connectionString, string dbName = "datasdb", string collectionName = "sampah")
        {
            var client = new MongoClient(connectionString);
            var db = client.GetDatabase(dbName);
            _collection = db.GetCollection<Sampah>(collectionName);
        }

        // --- Sync methods (existing) ---
        public List<Sampah> GetAll()
        {
            return _collection.Find(Builders<Sampah>.Filter.Empty).ToList();
        }

        public Sampah GetById(string id)
        {
            return _collection.Find(s => s.Id == id).FirstOrDefault();
        }

        public void Create(Sampah item)
        {
            _collection.InsertOne(item);
        }

        public void Update(Sampah item)
        {
            _collection.ReplaceOne(s => s.Id == item.Id, item);
        }

        public void Delete(string id)
        {
            _collection.DeleteOne(s => s.Id == id);
        }

        // --- Async methods (new) ---
        public async Task<List<Sampah>> GetAllAsync()
        {
            var cursor = await _collection.FindAsync(Builders<Sampah>.Filter.Empty).ConfigureAwait(false);
            return await cursor.ToListAsync().ConfigureAwait(false);
        }

        public async Task<Sampah> GetByIdAsync(string id)
        {
            var cursor = await _collection.FindAsync(s => s.Id == id).ConfigureAwait(false);
            return await cursor.FirstOrDefaultAsync().ConfigureAwait(false);
        }

        public async Task CreateAsync(Sampah item)
        {
            await _collection.InsertOneAsync(item).ConfigureAwait(false);
        }

        public async Task UpdateAsync(Sampah item)
        {
            await _collection.ReplaceOneAsync(s => s.Id == item.Id, item).ConfigureAwait(false);
        }

        public async Task DeleteAsync(string id)
        {
            await _collection.DeleteOneAsync(s => s.Id == id).ConfigureAwait(false);
        }
    }
}
