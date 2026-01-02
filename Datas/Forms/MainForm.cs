using Datas.Forms;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Datas
{
    public partial class MainForm : Form
    {
        private IMongoCollection<Sampah> collection;

        // Mistral chat
        private readonly HttpClient httpClient = new HttpClient();
        private readonly List<ChatMessage> conversation = new List<ChatMessage>();
        private const string MISTRAL_API_URL = "https://api.mistral.ai/v1/chat/completions";

        public MainForm()
        {
            InitializeComponent();
            KoneksiMongo();
            LoadData();
            InitSpecificControls();
            InitMistralSystemMessage();

            // OTOMATIS: Menghubungkan event klik tombol 'X' ke logika nomor 2
            this.FormClosing += MainForm_FormClosing;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            LoadData();
        }

        // ============================================================
        // LOGIKA KEMBALI KE LOGIN (SAAT TOMBOL X DIKLIK)
        // ============================================================
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Jika user menutup form secara manual (klik tombol X)
            if (e.CloseReason == CloseReason.UserClosing)
            {
                // Tampilkan kembali Form Login (Form1)
                Form1 loginForm = new Form1();
                loginForm.Show();

                // Biarkan MainForm tertutup
            }
        }

        // ===============================
        // KONEKSI MONGODB
        // ===============================
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

        // ===============================
        // LOAD DATA KE DATAGRIDVIEW
        // ===============================
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

        // Mengisi form saat tabel diklik
        private void FillFormFromSelection()
        {
            if (dataGridView1.CurrentRow == null) return;
            var item = dataGridView1.CurrentRow.DataBoundItem as Sampah;
            if (item == null) return;

            try
            {
                var comboJenis = GetControl<ComboBox>("comboBox3");
                if (comboJenis != null && !string.IsNullOrEmpty(item.Nama))
                    comboJenis.SelectedItem = item.Nama;

                var nudJumlah = GetControl<NumericUpDown>("numericUpDown1");
                if (nudJumlah != null)
                {
                    decimal val = item.Jumlah;
                    if (val > nudJumlah.Maximum) val = nudJumlah.Maximum;
                    if (val < nudJumlah.Minimum) val = nudJumlah.Minimum;
                    nudJumlah.Value = val;
                }

                var comboKab = GetControl<ComboBox>("comboBox1");
                if (comboKab != null && !string.IsNullOrEmpty(item.Lokasi))
                    comboKab.SelectedItem = item.Lokasi;
            }
            catch { }
        }

        private void DgvSelectionChanged(object sender, EventArgs e)
        {
            FillFormFromSelection();
        }

        // ===============================
        // INISIALISASI KONTROL
        // ===============================
        private void InitSpecificControls()
        {
            // 1. ComboBox Kabupaten
            var comboKab = GetControl<ComboBox>("comboBox1");
            if (comboKab != null)
            {
                comboKab.Items.Clear();
                comboKab.Items.AddRange(new string[] {
                    "Kabupaten Bandung", "Kabupaten Bekasi", "Kabupaten Bogor",
                    "Kabupaten Cirebon", "Kota Bandung", "Kota Bogor", "Kota Depok"
                });
                comboKab.DropDownStyle = ComboBoxStyle.DropDownList;
            }

            // 2. ComboBox Model AI
            var comboModel = GetControl<ComboBox>("comboBox2");
            if (comboModel != null)
            {
                comboModel.Items.Clear();
                comboModel.Items.AddRange(new string[] { "mistral-tiny", "mistral-small", "open-mistral-7b" });
                comboModel.SelectedIndex = 0;
                comboModel.DropDownStyle = ComboBoxStyle.DropDownList;
            }

            // 3. ComboBox Jenis Sampah
            var comboJenis = GetControl<ComboBox>("comboBox3");
            if (comboJenis != null)
            {
                comboJenis.Items.Clear();
                comboJenis.Items.AddRange(new string[] { "Organik", "Anorganik", "Plastik", "Kertas", "Logam", "B3" });
                comboJenis.DropDownStyle = ComboBoxStyle.DropDownList;
            }

            // Setup API Key (Masking)
            var apiBox = FindApiKeyBox();
            if (apiBox != null) apiBox.PasswordChar = '*';
        }

        private void InitMistralSystemMessage()
        {
            conversation.Clear();
            conversation.Add(new ChatMessage { role = "system", content = "Anda adalah AI asisten pengelolaan data sampah Jawa Barat." });
        }

        // Helper: Cari kontrol berdasarkan nama
        private T GetControl<T>(string name) where T : Control
        {
            return this.Controls.Find(name, true).FirstOrDefault() as T;
        }

        private TextBox FindApiKeyBox()
        {
            return GetControl<TextBox>("textBoxApiKey") ?? GetControl<TextBox>("textBox5");
        }

        // ===============================
        // CRUD: CREATE, UPDATE, DELETE
        // ===============================
        private void button1_Click(object sender, EventArgs e) // CREATE
        {
            try
            {
                var comboJenis = GetControl<ComboBox>("comboBox3");
                var nudJumlah = GetControl<NumericUpDown>("numericUpDown1");
                var comboKab = GetControl<ComboBox>("comboBox1");

                if (comboJenis?.SelectedItem == null || comboKab?.SelectedItem == null)
                {
                    MessageBox.Show("Mohon lengkapi Jenis Sampah dan Lokasi.");
                    return;
                }

                var s = new Sampah
                {
                    Nama = comboJenis.SelectedItem.ToString(),
                    Jumlah = (int)nudJumlah.Value,
                    Lokasi = comboKab.SelectedItem.ToString()
                };

                collection.InsertOne(s);
                LoadData();
                MessageBox.Show("Data berhasil ditambahkan.");
            }
            catch (Exception ex) { MessageBox.Show("Error Create: " + ex.Message); }
        }

        private void button2_Click(object sender, EventArgs e) // UPDATE
        {
            try
            {
                if (dataGridView1.CurrentRow == null) return;
                var current = dataGridView1.CurrentRow.DataBoundItem as Sampah;

                var comboJenis = GetControl<ComboBox>("comboBox3");
                var nudJumlah = GetControl<NumericUpDown>("numericUpDown1");
                var comboKab = GetControl<ComboBox>("comboBox1");

                var update = Builders<Sampah>.Update
                    .Set(x => x.Nama, comboJenis.SelectedItem.ToString())
                    .Set(x => x.Jumlah, (int)nudJumlah.Value)
                    .Set(x => x.Lokasi, comboKab.SelectedItem.ToString());

                collection.UpdateOne(x => x.Id == current.Id, update);
                LoadData();
                MessageBox.Show("Data berhasil diupdate.");
            }
            catch (Exception ex) { MessageBox.Show("Error Update: " + ex.Message); }
        }

        private void button3_Click(object sender, EventArgs e) // DELETE
        {
            try
            {
                if (dataGridView1.CurrentRow == null) return;
                var current = dataGridView1.CurrentRow.DataBoundItem as Sampah;
                if (MessageBox.Show("Hapus item ini?", "Konfirmasi", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    collection.DeleteOne(x => x.Id == current.Id);
                    LoadData();
                }
            }
            catch (Exception ex) { MessageBox.Show("Error Delete: " + ex.Message); }
        }

        private void button4_Click(object sender, EventArgs e) // CLEAR FORM
        {
            GetControl<ComboBox>("comboBox3").SelectedIndex = -1;
            GetControl<ComboBox>("comboBox1").SelectedIndex = -1;
            GetControl<NumericUpDown>("numericUpDown1").Value = 0;
        }

        // ===============================
        // MISTRAL AI CHAT
        // ===============================
        private async void button6_Click(object sender, EventArgs e)
        {
            var log = GetControl<RichTextBox>("richTextBox1");
            var input = GetControl<TextBox>("textBox4");
            var comboModel = GetControl<ComboBox>("comboBox2");

            if (string.IsNullOrEmpty(input.Text)) return;

            string userMsg = input.Text;
            log.AppendText("User: " + userMsg + Environment.NewLine);
            input.Clear();

            string apiKey = FindApiKeyBox()?.Text ?? "";
            string model = comboModel?.SelectedItem?.ToString() ?? "mistral-tiny";

            if (!string.IsNullOrEmpty(apiKey))
            {
                conversation.Add(new ChatMessage { role = "user", content = userMsg });
                try
                {
                    string reply = await GetMistralResponse(apiKey, model).ConfigureAwait(false);
                    this.Invoke((Action)(() => log.AppendText($"Mistral: {reply}{Environment.NewLine}")));
                }
                catch (Exception ex) { this.Invoke((Action)(() => log.AppendText("Error: " + ex.Message + Environment.NewLine))); }
            }
            else
            {
                log.AppendText("Bot Lokal: Halo! Masukkan API Key untuk chat pintar." + Environment.NewLine);
            }
        }

        private async Task<string> GetMistralResponse(string apiKey, string model)
        {
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + apiKey);
            var requestBody = new { model = model, messages = conversation, temperature = 0.7 };
            string json = JsonConvert.SerializeObject(requestBody);
            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            {
                var res = await httpClient.PostAsync(MISTRAL_API_URL, content).ConfigureAwait(false);
                var responseText = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!res.IsSuccessStatusCode) throw new Exception(responseText);
                dynamic result = JsonConvert.DeserializeObject(responseText);
                string reply = result.choices[0].message.content;
                conversation.Add(new ChatMessage { role = "assistant", content = reply });
                return reply;
            }
        }

        // ===============================
        // EXPORT PDF
        // ===============================
        private void button5_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog() { Filter = "PDF|*.pdf", FileName = "LaporanSampah.pdf" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    ExportToPdf(sfd.FileName);
                    MessageBox.Show("Export PDF Berhasil!");
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
                table.AddCell("Jenis Sampah"); table.AddCell("Jumlah"); table.AddCell("Lokasi");
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

        private void button7_Click(object sender, EventArgs e) // Clear Chat
        {
            GetControl<RichTextBox>("richTextBox1")?.Clear();
            InitMistralSystemMessage();
        }
    }

    public class ChatMessage
    {
        public string role { get; set; }
        public string content { get; set; }
    }
}