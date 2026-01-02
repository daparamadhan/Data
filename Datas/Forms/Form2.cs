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
        // Mendefinisikan collection di level class agar bisa diakses semua method
        private IMongoCollection<BsonDocument> collection;

        public Form2()
        {
            InitializeComponent();
            KoneksiMongo(); // Panggil koneksi sekali saat form dijalankan
        }

        private void KoneksiMongo()
        {
            try
            {
                // Pastikan connection string ini benar
                string connectionUri = "mongodb+srv://teman_dapa:ijal1234@jabar.mgjaupi.mongodb.net/?appName=Jabar";
                var client = new MongoClient(connectionUri);
                var database = client.GetDatabase("datas");
                collection = database.GetCollection<BsonDocument>("users");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal terhubung ke MongoDB: {ex.Message}", "Kesalahan Koneksi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Method ini sekarang sesuai dengan nama button1 di designer
        private void button1_Click(object sender, EventArgs e)
        {
            // Ambil data dari semua TextBox
            string nama = textBox1.Text;      // Input Nama
            string username = txtBoxUsername.Text;
            string password = txtBoxPassword.Text;
            string alamat = textBox4.Text;    // Input Alamat

            // Validasi: Pastikan semua field terisi
            if (!string.IsNullOrEmpty(nama) && !string.IsNullOrEmpty(username) &&
                !string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(alamat))
            {
                try
                {
                    // Cek apakah koneksi database sudah siap
                    if (collection == null) { KoneksiMongo(); }

                    // Cek apakah username sudah ada
                    var filter = Builders<BsonDocument>.Filter.Eq("username", username);
                    var existingUser = collection.Find(filter).FirstOrDefault();

                    if (existingUser == null)
                    {
                        // Buat dokumen baru dengan semua field
                        var newUser = new BsonDocument
                        {
                            { "nama", nama },
                            { "username", username },
                            { "password", password },
                            { "alamat", alamat },
                            { "created_at", DateTime.Now }
                        };

                        // Simpan ke MongoDB
                        collection.InsertOne(newUser);

                        MessageBox.Show("Akun berhasil dibuat!", "Registrasi Berhasil", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // Pindah ke MainForm
                        MainForm mainForm = new MainForm();
                        mainForm.Show();
                        this.Hide();
                    }
                    else
                    {
                        MessageBox.Show("Username sudah digunakan. Silakan gunakan username lain.", "Registrasi Gagal", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Terjadi kesalahan sistem: {ex.Message}", "Kesalahan", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Semua data (Nama, Username, Password, Alamat) wajib diisi!", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}