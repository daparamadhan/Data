using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MongoDB.Driver;
using MongoDB.Bson;

namespace Datas.Forms
{
    public partial class Form1 : Form
    {
        private IMongoCollection<BsonDocument> collection;

        public Form1()
        {
            InitializeComponent();
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            Form2 form2 = new Form2();
            form2.Show();
            this.Hide();
        }

        private void KoneksiMongo()
        {
            string connectionUri = "mongodb+srv://teman_dapa:ijal1234@jabar.mgjaupi.mongodb.net/?appName=Jabar";
            var client = new MongoClient(connectionUri);
            var database = client.GetDatabase("datas");
            collection = database.GetCollection<BsonDocument>("users");
        }

        private void buttonLogin_Click(object sender, EventArgs e)
        {
            KoneksiMongo();

            string username = txtBoxUsername.Text;
            string password = txtBoxPassword.Text;

            var filter = Builders<BsonDocument>.Filter.Eq("username", username) & Builders<BsonDocument>.Filter.Eq("password", password);
            var user = collection.Find(filter).FirstOrDefault();

            if (user != null)
            {
                MainForm mainForm = new MainForm();
                mainForm.Show();
                this.Hide();
            }
            else
            {
                MessageBox.Show("Akun tidak ditemukan. Silakan registrasi.", "Login Gagal", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Form2 form2 = new Form2();
                form2.Show();
                this.Hide();
            }
        }

        private void buttonRegister_Click(object sender, EventArgs e)
        {
            Form2 form2 = new Form2();
            form2.Show();
            this.Hide();
        }
    }
}
