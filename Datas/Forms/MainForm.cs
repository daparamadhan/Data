using System;
using System.Windows.Forms;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

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

            // Inisialisasi Kontrol sesuai permintaan spesifik
            InitSpecificControls();
            InitMistralSystemMessage();
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
            string connectionUri = "mongodb+srv://teman_dapa:ijal1234@jabar.mgjaupi.mongodb.net/?appName=Jabar";
            var client = new MongoClient(connectionUri);
            var database = client.GetDatabase("datas");
            collection = database.GetCollection<Sampah>("sampah");
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

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // Handled in SelectionChanged
        }

        private void DgvSelectionChanged(object sender, EventArgs e)
        {
            FillFormFromSelection();
        }

        // Mengisi form saat tabel diklik
        private void FillFormFromSelection()
        {
            if (dataGridView1.CurrentRow == null) return;
            var item = dataGridView1.CurrentRow.DataBoundItem as Sampah;
            if (item == null) return;

            try
            {
                // 1. Set ComboBox3 (Jenis Sampah)
                var comboJenis = GetControl<ComboBox>("comboBox3");
                if (comboJenis != null)
                {
                    if (!string.IsNullOrEmpty(item.Nama) && comboJenis.Items.Contains(item.Nama))
                        comboJenis.SelectedItem = item.Nama;
                    else
                        comboJenis.SelectedIndex = -1;
                }

                // 2. Set NumericUpDown (Jumlah) -> PERUBAHAN DISINI
                var nudJumlah = GetControl<NumericUpDown>("numericUpDown1");
                if (nudJumlah != null)
                {
                    // Pastikan nilai tidak melebihi batas Max/Min kontrol numeric
                    decimal val = item.Jumlah;
                    if (val > nudJumlah.Maximum) val = nudJumlah.Maximum;
                    if (val < nudJumlah.Minimum) val = nudJumlah.Minimum;

                    nudJumlah.Value = val;
                }

                // 3. Set ComboBox1 (Kabupaten/Lokasi)
                var comboKab = GetControl<ComboBox>("comboBox1");
                if (comboKab != null)
                {
                    if (!string.IsNullOrEmpty(item.Lokasi) && comboKab.Items.Contains(item.Lokasi))
                        comboKab.SelectedItem = item.Lokasi;
                    else
                        comboKab.SelectedIndex = -1;
                }
            }
            catch { }
        }

        // ===============================
        // INISIALISASI KONTROL (MAPPING BARU)
        // ===============================
        private void InitSpecificControls()
        {
            // ---------------------------------------------------------
            // 1. MAPPING COMBOBOX 1 -> KABUPATEN
            // ---------------------------------------------------------
            var comboKab = GetControl<ComboBox>("comboBox1");
            if (comboKab != null)
            {
                comboKab.Items.Clear();
                string[] kabupatenList = new string[] {
                    "Kabupaten Bandung", "Kabupaten Bandung Barat", "Kabupaten Bekasi",
                    "Kabupaten Bogor", "Kabupaten Ciamis", "Kabupaten Cianjur",
                    "Kabupaten Cirebon", "Kabupaten Garut", "Kabupaten Indramayu",
                    "Kabupaten Karawang", "Kabupaten Kuningan", "Kabupaten Majalengka",
                    "Kabupaten Pangandaran", "Kabupaten Purwakarta", "Kabupaten Subang",
                    "Kabupaten Sukabumi", "Kabupaten Sumedang", "Kabupaten Tasikmalaya",
                    "Kota Bandung", "Kota Bogor", "Kota Bekasi", "Kota Cirebon", "Kota Depok"
                };
                comboKab.Items.AddRange(kabupatenList);
                comboKab.DropDownStyle = ComboBoxStyle.DropDownList; // Agar user tidak bisa ketik manual
            }

            // ---------------------------------------------------------
            // 2. MAPPING COMBOBOX 2 -> MODEL MISTRAL AI
            // ---------------------------------------------------------
            var comboModel = GetControl<ComboBox>("comboBox2");
            if (comboModel != null)
            {
                comboModel.Items.Clear();
                comboModel.Items.AddRange(new string[] { "mistral-tiny", "mistral-small", "open-mistral-7b" });
                comboModel.SelectedIndex = 0; // Default select index 0
                comboModel.DropDownStyle = ComboBoxStyle.DropDownList;
            }

            // ---------------------------------------------------------
            // 3. MAPPING COMBOBOX 3 -> JENIS SAMPAH
            // ---------------------------------------------------------
            var comboJenis = GetControl<ComboBox>("comboBox3");
            if (comboJenis != null)
            {
                comboJenis.Items.Clear();
                comboJenis.Items.AddRange(new string[] {
                    "Organik", "Anorganik", "Plastik", "Kertas",
                    "Logam", "Kaca", "Elektronik", "B3 (Bahan Berbahaya dan Beracun)"
                });
                comboJenis.SelectedIndex = -1;
                comboJenis.DropDownStyle = ComboBoxStyle.DropDownList;
            }

            // Setup API Key Box (Masking Password)
            var apiBox = FindApiKeyBox();
            if (apiBox != null) apiBox.PasswordChar = '*';

            // Setup Tombol Clear Chat
            var clearBtn = this.Controls.OfType<Button>().FirstOrDefault(b => b.Text != null && (b.Text.Equals("Clear") || b.Text.Equals("Bersihkan")));
            if (clearBtn != null)
            {
                clearBtn.Click += (s, e) =>
                {
                    var log = GetControl<RichTextBox>("richTextBox1");
                    if (log != null) log.Clear();
                    InitMistralSystemMessage();
                };
            }
        }

        private void InitMistralSystemMessage()
        {
            conversation.Clear();
            conversation.Add(new ChatMessage { role = "system", content = "Anda adalah AI asisten pengelolaan data sampah Jawa Barat." });
        }

        // ===============================
        // HELPER: Get Control by Name
        // ===============================
        private T GetControl<T>(string name) where T : Control
        {
            return this.Controls.Find(name, true).FirstOrDefault() as T;
        }

        private TextBox FindApiKeyBox()
        {
            // Cari textbox untuk API Key (bisa disesuaikan nama kontrolnya)
            return GetControl<TextBox>("textBoxApiKey") ??
                   GetControl<TextBox>("textBoxApi") ??
                   GetControl<TextBox>("textBox5") ?? // Asumsi textBox5
                   GetControl<TextBox>("textBox1");   // Fallback jika textBox1 dipakai untuk API
        }

        // ===============================
        // CRUD: CREATE (Button 1)
        // ===============================
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                // Ambil kontrol spesifik
                var comboJenis = GetControl<ComboBox>("comboBox3"); // Jenis
                var nudJumlah = GetControl<NumericUpDown>("numericUpDown1"); // PERUBAHAN: NumericUpDown
                var comboKab = GetControl<ComboBox>("comboBox1");   // Kabupaten

                if (comboJenis == null || nudJumlah == null || comboKab == null)
                {
                    MessageBox.Show("Kontrol form tidak lengkap (cek nama numericUpDown1).");
                    return;
                }

                if (comboJenis.SelectedItem == null) { MessageBox.Show("Pilih Jenis Sampah (Combo 3)"); return; }
                if (comboKab.SelectedItem == null) { MessageBox.Show("Pilih Kabupaten (Combo 1)"); return; }

                // Tidak perlu TryParse karena NumericUpDown sudah pasti angka
                int jumlah = (int)nudJumlah.Value;

                var s = new Sampah
                {
                    Nama = comboJenis.SelectedItem.ToString(),
                    Jumlah = jumlah,
                    Lokasi = comboKab.SelectedItem.ToString()
                };

                collection.InsertOne(s);
                LoadData();
                MessageBox.Show("Data berhasil ditambahkan.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error Create: " + ex.Message);
            }
        }

        // ===============================
        // CRUD: UPDATE (Button 2)
        // ===============================
        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                if (dataGridView1.CurrentRow == null) { MessageBox.Show("Pilih data di tabel dulu."); return; }
                var current = dataGridView1.CurrentRow.DataBoundItem as Sampah;
                if (current == null) return;

                var comboJenis = GetControl<ComboBox>("comboBox3");
                var nudJumlah = GetControl<NumericUpDown>("numericUpDown1"); // PERUBAHAN: NumericUpDown
                var comboKab = GetControl<ComboBox>("comboBox1");

                if (comboJenis.SelectedItem == null) { MessageBox.Show("Pilih Jenis Sampah"); return; }
                if (comboKab.SelectedItem == null) { MessageBox.Show("Pilih Kabupaten"); return; }

                // Tidak perlu TryParse
                int jumlah = (int)nudJumlah.Value;

                var update = Builders<Sampah>.Update
                    .Set(x => x.Nama, comboJenis.SelectedItem.ToString())
                    .Set(x => x.Jumlah, jumlah)
                    .Set(x => x.Lokasi, comboKab.SelectedItem.ToString());

                collection.UpdateOne(x => x.Id == current.Id, update);
                LoadData();
                MessageBox.Show("Data berhasil diupdate.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error Update: " + ex.Message);
            }
        }

        // ===============================
        // CRUD: DELETE (Button 3)
        // ===============================
        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                if (dataGridView1.CurrentRow == null) { MessageBox.Show("Pilih data dulu"); return; }
                var current = dataGridView1.CurrentRow.DataBoundItem as Sampah;
                if (current == null) return;

                if (MessageBox.Show("Hapus item ini?", "Konfirmasi", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    collection.DeleteOne(x => x.Id == current.Id);
                    LoadData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error Delete: " + ex.Message);
            }
        }

        // ===============================
        // CLEAR FORM (Button 4)
        // ===============================
        private void button4_Click(object sender, EventArgs e)
        {
            var comboJenis = GetControl<ComboBox>("comboBox3");
            var nudJumlah = GetControl<NumericUpDown>("numericUpDown1"); // PERUBAHAN
            var comboKab = GetControl<ComboBox>("comboBox1");
            var comboModel = GetControl<ComboBox>("comboBox2");

            if (comboJenis != null) comboJenis.SelectedIndex = -1;
            if (comboKab != null) comboKab.SelectedIndex = -1;

            // PERUBAHAN: Reset NumericUpDown ke 0
            if (nudJumlah != null) nudJumlah.Value = 0;

            // Opsional: Reset model ke default
            if (comboModel != null && comboModel.Items.Count > 0) comboModel.SelectedIndex = 0;
        }

        // ===============================
        // MISTRAL AI CHAT (Button 6)
        // ===============================
        private async void button6_Click(object sender, EventArgs e)
        {
            var log = GetControl<RichTextBox>("richTextBox1");
            var input = GetControl<TextBox>("textBox4");

            // AMBIL MODEL DARI COMBOBOX 2
            var comboModel = GetControl<ComboBox>("comboBox2");

            if (log == null || input == null) return;
            string userMsg = input.Text.Trim();
            if (string.IsNullOrEmpty(userMsg)) return;

            log.AppendText("User: " + userMsg + Environment.NewLine);
            input.Clear();

            var apiBox = FindApiKeyBox();
            string apiKey = apiBox != null ? apiBox.Text : "";

            // Validasi apakah model dipilih dari ComboBox 2
            string selectedModel = (comboModel != null && comboModel.SelectedItem != null)
                                                ? comboModel.SelectedItem.ToString()
                                                : "mistral-tiny"; // Fallback default

            if (!string.IsNullOrEmpty(apiKey))
            {
                // Panggil API Mistral
                conversation.Add(new ChatMessage { role = "user", content = userMsg });
                try
                {
                    string reply = await GetMistralResponse(apiKey, selectedModel).ConfigureAwait(false);
                    this.Invoke((Action)(() => log.AppendText($"Mistral ({selectedModel}): {reply}{Environment.NewLine}")));
                }
                catch (Exception ex)
                {
                    this.Invoke((Action)(() => log.AppendText("Error API: " + ex.Message + Environment.NewLine)));
                }
            }
            else
            {
                // Fallback Chatbot Lokal
                string reply = GetChatbotResponse(userMsg);
                log.AppendText("Bot Lokal: " + reply + Environment.NewLine);
            }
        }

        private async Task<string> GetMistralResponse(string apiKey, string model)
        {
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + apiKey);

            var requestBody = new
            {
                model = model,
                messages = conversation,
                temperature = 0.7
            };

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

        private string GetChatbotResponse(string input)
        {
            input = input.ToLower();
            if (input.Contains("halo")) return "Halo! Masukkan API Key untuk chat pintar.";
            if (input.Contains("jumlah")) return "Total data: " + collection.CountDocuments(_ => true);
            return "Saya bot sederhana. Gunakan Mistral API untuk fitur lebih lengkap.";
        }

        // ===============================
        // EXPORT PDF (Button 5)
        // ===============================
        private void button5_Click(object sender, EventArgs e)
        {
            try
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
            catch (Exception ex) { MessageBox.Show("Gagal Export: " + ex.Message); }
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
                table.AddCell("Jenis Sampah");
                table.AddCell("Jumlah (Kg/Ton)");
                table.AddCell("Kabupaten/Lokasi");

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

        // Event handler kosong
        private void textBox3_TextChanged(object sender, EventArgs e) { } // Sudah tidak dipakai, tapi dibiarkan agar tidak error designer
        private void textBox4_TextChanged(object sender, EventArgs e) { }
        private void richTextBox1_TextChanged(object sender, EventArgs e) { }
        private void dateTimePicker1_ValueChanged(object sender, EventArgs e) { }
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e) { }
        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e) { }
        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e) { }
        private void button7_Click(object sender, EventArgs e)
        {
            var log = GetControl<RichTextBox>("richTextBox1");
            if (log != null) log.Clear();
            InitMistralSystemMessage();
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {

        }
    }

    public class ChatMessage
    {
        public string role { get; set; }
        public string content { get; set; }
    }
}