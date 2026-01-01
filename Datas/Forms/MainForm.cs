using System;
using System.Windows.Forms;
using MongoDB.Driver;
using System.Collections.Generic;

namespace Datas
{
    public partial class MainForm : Form
    {
        private IMongoCollection<Sampah> collection;

        public MainForm()
        {
            InitializeComponent();
            KoneksiMongo();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            LoadData();
        }

        // ===============================
        // KONEKSI MONGODB
        // ===============================
        private void KoneksiMongo()
        {
            string connectionUri =
                "mongodb+srv://daparamdnnn_db_user:rxgTiPOuzcB5lFRe@jabar.mgjaupi.mongodb.net/?appName=Jabar";

            var client = new MongoClient(connectionUri);

            var database = client.GetDatabase("datas");      // DATABASE BARU
            collection = database.GetCollection<Sampah>("sampah"); // COLLECTION BARU
        }

        // ===============================
        // LOAD DATA KE DATAGRIDVIEW
        // ===============================
        private void LoadData()
        {
            var data = collection.Find(_ => true).ToList();
            dataGridView1.DataSource = data;
        }
    }
}
