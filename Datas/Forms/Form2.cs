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
    public partial class Form2 : Form
    {
        private IMongoCollection<BsonDocument> collection;

        public Form2()
        {
            InitializeComponent();
        }

        private void KoneksiMongo()
        {
             try
             {
                 string connectionUri = "mongodb+srv://teman_dapa:ijal1234@jabar.mgjaupi.mongodb.net/?appName=Jabar";
                 var client = new MongoClient(connectionUri);
                 var database = client.GetDatabase("datas");
                 collection = database.GetCollection<BsonDocument>("users");
                 MessageBox.Show("Koneksi ke MongoDB berhasil.", "Informasi", MessageBoxButtons.OK, MessageBoxIcon.Information);
             }
             catch (Exception ex)
             {
                 MessageBox.Show($"Gagal terhubung ke MongoDB: {ex.Message}", "Kesalahan", MessageBoxButtons.OK, MessageBoxIcon.Error);
             }
        }

        private void buttonRegister_Click(object sender, EventArgs e)
        {
            KoneksiMongo();

            string username = txtBoxUsername.Text;
            string password = txtBoxPassword.Text;

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                try
                {
                    var filter = Builders<BsonDocument>.Filter.Eq("username", username);
                    var existingUser = collection.Find(filter).FirstOrDefault();

                    if (existingUser == null)
                    {
                        var newUser = new BsonDocument
                        {
                            { "username", username },
                            { "password", password }
                        };
                        collection.InsertOne(newUser);

                        MessageBox.Show("Akun berhasil dibuat.", "Registrasi Berhasil", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        MainForm mainForm = new MainForm();
                        mainForm.Show();
                        this.Hide();
                    }
                    else
                    {
                        MessageBox.Show("Username sudah digunakan. Silakan gunakan username lain.", "Registrasi Gagal", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Terjadi kesalahan saat menyimpan data: {ex.Message}", "Kesalahan", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Username dan password tidak boleh kosong.", "Registrasi Gagal", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
