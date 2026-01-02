using Datas.Forms;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization; // Penting untuk format angka desimal (0.7)
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Datas
{
    public partial class MainForm : Form
    {
        // ==========================================
        // VARIABEL GLOBAL
        // ==========================================
        private IMongoCollection<Sampah> collection;

        // Variabel Chat Mistral
        private readonly HttpClient httpClient = new HttpClient();
        private readonly List<ChatMessage> conversation = new List<ChatMessage>();
        private const string MISTRAL_API_URL = "https://api.mistral.ai/v1/chat/completions";

        public MainForm()
        {
            InitializeComponent();

            // Setup Database & UI Awal
            KoneksiMongo();
            LoadData();
            InitSpecificControls();
            InitMistralSystemMessage();

            // Event saat form ditutup (Logout logika)
            this.FormClosing += MainForm_FormClosing;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            LoadData();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                Form1 loginForm = new Form1();
                loginForm.Show();
            }
        }

        // ==========================================
        // 1. DATABASE MONGODB & CRUD
        // ==========================================
        private void KoneksiMongo()
        {
            try
            {
                string connectionUri = "mongodb+srv://teman_dapa:ijal1234@jabar.mgjaupi.mongodb.net/?appName=Jabar";
                var client = new MongoClient(connectionUri);
                var database = client.GetDatabase("datas");
                collection = database.GetCollection<Sampah>("sampah");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal koneksi ke database: " + ex.Message);
            }
        }

        private void LoadData()
        {
            try
            {
                var data = collection.Find(_ => true).ToList();
                dataGridView1.DataSource = data;
                if (dataGridView1.Columns.Contains("Id"))
                    dataGridView1.Columns["Id"].Visible = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal load data: " + ex.Message);
            }
        }

        private void DgvSelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null) return;
            var item = dataGridView1.CurrentRow.DataBoundItem as Sampah;
            if (item == null) return;

            try
            {
                GetControl<ComboBox>("comboBox3").SelectedItem = item.Nama;
                GetControl<NumericUpDown>("numericUpDown1").Value = item.Jumlah;
                GetControl<ComboBox>("comboBox1").SelectedItem = item.Lokasi;
            }
            catch { }
        }

        // CRUD BUTTONS
        private void button1_Click(object sender, EventArgs e) // CREATE
        {
            try
            {
                var s = new Sampah
                {
                    Nama = GetControl<ComboBox>("comboBox3").SelectedItem?.ToString(),
                    Jumlah = (int)GetControl<NumericUpDown>("numericUpDown1").Value,
                    Lokasi = GetControl<ComboBox>("comboBox1").SelectedItem?.ToString()
                };
                if (s.Nama == null || s.Lokasi == null) { MessageBox.Show("Lengkapi data!"); return; }

                collection.InsertOne(s);
                LoadData();
                MessageBox.Show("Data berhasil ditambahkan.");
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void button2_Click(object sender, EventArgs e) // UPDATE
        {
            try
            {
                if (dataGridView1.CurrentRow == null) return;
                var current = dataGridView1.CurrentRow.DataBoundItem as Sampah;

                var update = Builders<Sampah>.Update
                    .Set(x => x.Nama, GetControl<ComboBox>("comboBox3").SelectedItem?.ToString())
                    .Set(x => x.Jumlah, (int)GetControl<NumericUpDown>("numericUpDown1").Value)
                    .Set(x => x.Lokasi, GetControl<ComboBox>("comboBox1").SelectedItem?.ToString());

                collection.UpdateOne(x => x.Id == current.Id, update);
                LoadData();
                MessageBox.Show("Data berhasil diupdate.");
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void button3_Click(object sender, EventArgs e) // DELETE
        {
            try
            {
                if (dataGridView1.CurrentRow == null) return;
                var current = dataGridView1.CurrentRow.DataBoundItem as Sampah;
                if (MessageBox.Show("Hapus item?", "Konfirmasi", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    collection.DeleteOne(x => x.Id == current.Id);
                    LoadData();
                }
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void button4_Click(object sender, EventArgs e) // CLEAR FORM CRUD
        {
            GetControl<ComboBox>("comboBox3").SelectedIndex = -1;
            GetControl<ComboBox>("comboBox1").SelectedIndex = -1;
            GetControl<NumericUpDown>("numericUpDown1").Value = 0;
        }

        private void button5_Click(object sender, EventArgs e) // EXPORT PDF
        {
            using (SaveFileDialog sfd = new SaveFileDialog() { Filter = "PDF|*.pdf", FileName = "LaporanSampah.pdf" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    ExportToPdf(sfd.FileName);
                    MessageBox.Show("Export Berhasil!");
                }
            }
        }

        private void ExportToPdf(string path)
        {
            var list = collection.Find(_ => true).ToList();
            using (var fs = new System.IO.FileStream(path, System.IO.FileMode.Create))
            {
                var doc = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 25, 25, 30, 30);
                iTextSharp.text.pdf.PdfWriter.GetInstance(doc, fs);
                doc.Open();
                doc.Add(new iTextSharp.text.Paragraph("Laporan Data Sampah Jawa Barat"));
                doc.Add(new iTextSharp.text.Paragraph("\n"));
                var table = new iTextSharp.text.pdf.PdfPTable(3);
                table.AddCell("Jenis"); table.AddCell("Jumlah"); table.AddCell("Lokasi");
                foreach (var item in list)
                {
                    table.AddCell(item.Nama ?? "-");
                    table.AddCell(item.Jumlah.ToString());
                    table.AddCell(item.Lokasi ?? "-");
                }
                doc.Add(table);
                doc.Close();
            }
        }

        // ==========================================
        // 2. LOGIKA MISTRAL AI (SUDAH DIPERBAIKI VERSI C# & DATA KOTA)
        // ==========================================

        private void InitSpecificControls()
        {
            var comboModel = GetControl<ComboBox>("comboBox2");
            if (comboModel != null)
            {
                comboModel.Items.Clear();
                comboModel.Items.Add("mistral-tiny");
                comboModel.Items.Add("mistral-small");
                comboModel.Items.Add("open-mistral-7b");
                comboModel.SelectedIndex = 2;
                comboModel.DropDownStyle = ComboBoxStyle.DropDownList;
            }

            // --- UPDATE DATA KABUPATEN/KOTA LENGKAP JAWA BARAT ---
            var comboKab = GetControl<ComboBox>("comboBox1");
            if (comboKab != null)
            {
                comboKab.Items.Clear();
                comboKab.Items.AddRange(new string[] { 
                    // Kabupaten
                    "Kab. Bandung", "Kab. Bandung Barat", "Kab. Bekasi", "Kab. Bogor",
                    "Kab. Ciamis", "Kab. Cianjur", "Kab. Cirebon", "Kab. Garut",
                    "Kab. Indramayu", "Kab. Karawang", "Kab. Kuningan", "Kab. Majalengka",
                    "Kab. Pangandaran", "Kab. Purwakarta", "Kab. Subang", "Kab. Sukabumi",
                    "Kab. Sumedang", "Kab. Tasikmalaya",
                    // Kota
                    "Kota Bandung", "Kota Banjar", "Kota Bekasi", "Kota Bogor",
                    "Kota Cimahi", "Kota Cirebon", "Kota Depok", "Kota Sukabumi", "Kota Tasikmalaya"
                });
                comboKab.DropDownStyle = ComboBoxStyle.DropDownList;
            }

            var comboJenis = GetControl<ComboBox>("comboBox3");
            if (comboJenis != null)
            {
                comboJenis.Items.AddRange(new string[] { "Organik", "Anorganik", "Plastik", "B3" });
                comboJenis.DropDownStyle = ComboBoxStyle.DropDownList;
            }

            var apiBox = GetControl<TextBox>("textBox1");
            if (apiBox != null) apiBox.PasswordChar = '*';
        }

        private void InitMistralSystemMessage()
        {
            conversation.Clear();
            conversation.Add(new ChatMessage
            {
                role = "system",
                content = "Anda adalah AI asisten pengelolaan sampah yang ramah dan membantu."
            });
        }

        private async void button6_Click(object sender, EventArgs e)
        {
            var txtApiKey = GetControl<TextBox>("textBox1");
            var txtMessage = GetControl<TextBox>("textBox4");
            var txtConversation = GetControl<RichTextBox>("richTextBox1");
            var cmbModel = GetControl<ComboBox>("comboBox2");

            if (string.IsNullOrWhiteSpace(txtApiKey.Text))
            {
                MessageBox.Show("API Key belum diisi.", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string userMessage = txtMessage.Text;
            if (string.IsNullOrWhiteSpace(userMessage) || userMessage.Replace("\r", "").Replace("\n", "").Length == 0)
            {
                MessageBox.Show("Pesan tidak boleh kosong.", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            userMessage = userMessage.Trim();
            conversation.Add(new ChatMessage { role = "user", content = userMessage });

            txtConversation.AppendText($"👤 Anda: {userMessage}{Environment.NewLine}{Environment.NewLine}");
            txtMessage.Clear();
            txtConversation.ScrollToCaret();

            try
            {
                txtConversation.AppendText("🤖 AI: (Sedang mengetik...)" + Environment.NewLine);

                string reply = await GetMistralResponse(txtApiKey.Text, cmbModel.SelectedItem.ToString());

                txtConversation.AppendText($"🤖 AI: {reply}{Environment.NewLine}{Environment.NewLine}");
                txtConversation.ScrollToCaret();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal mendapatkan respon:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // --- REQUEST API (BAGIAN YG DIPERBAIKI UNTUK C# 7.3) ---
        private async Task<string> GetMistralResponse(string apiKey, string modelName)
        {
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "JabarCleanApp/1.0");
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + apiKey.Trim());

            var jsonSettings = new JsonSerializerSettings { Culture = CultureInfo.InvariantCulture };

            var requestBody = new
            {
                model = modelName,
                messages = conversation,
                temperature = 0.7
            };

            string json = JsonConvert.SerializeObject(requestBody, jsonSettings);

            // FIX: Menggunakan 'using' dengan kurung kurawal {}
            // Ini menggantikan 'using var' yang menyebabkan error di C# versi lama
            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            {
                HttpResponseMessage response = await httpClient.PostAsync(MISTRAL_API_URL, content);
                string responseText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"API Error ({response.StatusCode}): {responseText}");

                dynamic result = JsonConvert.DeserializeObject(responseText);

                if (result?.choices == null || result.choices.Count == 0)
                    throw new Exception("Response kosong dari server.");

                string reply = result.choices[0].message.content;

                conversation.Add(new ChatMessage
                {
                    role = "assistant",
                    content = reply
                });

                return reply;
            } // End using content
        }

        private void button7_Click(object sender, EventArgs e)
        {
            var txtConversation = GetControl<RichTextBox>("richTextBox1");
            var txtMessage = GetControl<TextBox>("textBox4");

            txtConversation.Clear();
            txtMessage.Clear();
            InitMistralSystemMessage();
            MessageBox.Show("Percakapan dibersihkan.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private T GetControl<T>(string name) where T : Control
        {
            return this.Controls.Find(name, true).FirstOrDefault() as T;
        }
    }

    public class ChatMessage
    {
        public string role { get; set; } = string.Empty;
        public string content { get; set; } = string.Empty;
    }
}