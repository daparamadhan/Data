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

            InitMistralControls();
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
            string connectionUri =
                "mongodb+srv://teman_dapa:ijal1234@jabar.mgjaupi.mongodb.net/?appName=Jabar";

            var client = new MongoClient(connectionUri);

            var database = client.GetDatabase("datas");      // DATABASE BARU
            collection = database.GetCollection<Sampah>("sampah"); // COLLECTION BARU
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
                // adjust columns if present
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
            // No-op; selection handled in SelectionChanged event
        }

        private void DgvSelectionChanged(object sender, EventArgs e)
        {
            // helper in case designer wired different event; keep both
            FillFormFromSelection();
        }

        private void FillFormFromSelection()
        {
            if (dataGridView1.CurrentRow == null) return;
            var item = dataGridView1.CurrentRow.DataBoundItem as Sampah;
            if (item == null) return;

            try
            {
                var jenisCombo = GetControl<ComboBox>("comboBox3");
                var tbJumlah = GetControl<TextBox>("textBox3");

                if (jenisCombo != null)
                {
                    // select matched jenis if present
                    if (!string.IsNullOrEmpty(item.Nama) && jenisCombo.Items.Contains(item.Nama))
                        jenisCombo.SelectedItem = item.Nama;
                    else
                        jenisCombo.SelectedIndex = -1;
                }

                if (tbJumlah != null) tbJumlah.Text = item.Jumlah.ToString();

                // Prefer kabupaten combobox if present
                var kabCombo = FindKabupatenCombo();
                if (kabCombo != null)
                {
                    kabCombo.SelectedItem = item.Lokasi;
                }
            }
            catch { }
        }

        // Generic helper to find control by name
        private T GetControl<T>(string name) where T : Control
        {
            return this.Controls.Find(name, true).FirstOrDefault() as T;
        }

        // Helper to find API key textbox
        private TextBox FindApiKeyBox()
        {
            var apiBox = GetControl<TextBox>("textBoxApiKey") ??
                         GetControl<TextBox>("textBox5") ??
                         GetControl<TextBox>("textBoxApi") ??
                         GetControl<TextBox>("textBox1"); // textBox1 in designer holds the API key

            if (apiBox == null)
            {
                // fallback: find first TextBox with PasswordChar
                apiBox = this.Controls.OfType<TextBox>().FirstOrDefault(t => t.PasswordChar != '\0');
            }

            return apiBox;
        }

        // Helper: find model combobox (prefer Tag, name hints, or empty-items)
        private ComboBox FindModelCombo()
        {
            var combos = this.Controls.OfType<ComboBox>().ToList();
            // prefer explicit tag
            var byTag = combos.FirstOrDefault(cb => cb.Tag != null && cb.Tag.ToString() == "model");
            if (byTag != null) return byTag;

            // prefer name hints
            var byName = combos.FirstOrDefault(cb => cb.Name.IndexOf("model", StringComparison.OrdinalIgnoreCase) >= 0
                                                   || cb.Name.IndexOf("mistral", StringComparison.OrdinalIgnoreCase) >= 0);
            if (byName != null) return byName;

            // prefer combobox with no items (we will populate model items)
            var emptyItems = combos.FirstOrDefault(cb => cb.Items == null || cb.Items.Count == 0);
            if (emptyItems != null) return emptyItems;

            // fallback to first combobox
            return combos.FirstOrDefault();
        }

        // Helper: find kabupaten combobox (prefer Tag or name hints)
        private ComboBox FindKabupatenCombo()
        {
            var combos = this.Controls.OfType<ComboBox>().ToList();
            var byTag = combos.FirstOrDefault(cb => cb.Tag != null && cb.Tag.ToString() == "kabupaten");
            if (byTag != null) return byTag;

            var byName = combos.FirstOrDefault(cb => cb.Name.IndexOf("kab", StringComparison.OrdinalIgnoreCase) >= 0
                                                    || cb.Name.IndexOf("kabupaten", StringComparison.OrdinalIgnoreCase) >= 0);
            if (byName != null) return byName;

            // pick a combobox different from model if possible
            var model = FindModelCombo();
            return combos.FirstOrDefault(cb => cb != model) ?? model;
        }

        private void InitMistralControls()
        {
            // determine comboboxes deterministically and mark them with Tag
            var modelCombo = FindModelCombo();
            var kabupatenCombo = FindKabupatenCombo();

            if (modelCombo != null)
            {
                modelCombo.Tag = "model";
                modelCombo.Items.Clear();
                modelCombo.Items.AddRange(new string[] { "mistral-tiny", "mistral-small", "open-mistral-7b" });
                modelCombo.SelectedIndex = 0;
            }

            if (kabupatenCombo != null)
            {
                kabupatenCombo.Tag = "kabupaten";
                string[] kabupatenList = new string[] {
                    "Kabupaten Bandung",
                    "Kabupaten Bandung Barat",
                    "Kabupaten Bekasi",
                    "Kabupaten Bogor",
                    "Kabupaten Ciamis",
                    "Kabupaten Cianjur",
                    "Kabupaten Cirebon",
                    "Kabupaten Garut",
                    "Kabupaten Indramayu",
                    "Kabupaten Karawang",
                    "Kabupaten Kuningan",
                    "Kabupaten Majalengka",
                    "Kabupaten Pangandaran",
                    "Kabupaten Purwakarta",
                    "Kabupaten Subang",
                    "Kabupaten Sukabumi",
                    "Kabupaten Sumedang",
                    "Kabupaten Tasikmalaya"
                };

                kabupatenCombo.Items.Clear();
                kabupatenCombo.Items.AddRange(kabupatenList);
                kabupatenCombo.SelectedIndex = -1;
            }

            // populate jenis sampah in comboBox3
            var jenisCombo = GetControl<ComboBox>("comboBox3");
            if (jenisCombo != null)
            {
                jenisCombo.Items.Clear();
                jenisCombo.Items.AddRange(new string[] {
                    "Organik",
                    "Anorganik",
                    "Plastik",
                    "Kertas",
                    "Logam",
                    "Kaca",
                    "Elektronik",
                    "B3 (Bahan Berbahaya dan Beracun)"
                });
                jenisCombo.SelectedIndex = -1;
            }

            // find API key textbox by name or by masked char
            var apiBox = FindApiKeyBox();
            if (apiBox != null) apiBox.PasswordChar = '*';

            // wire Clear button next to chat area: look for button with text "Clear" or "Bersihkan"
            var clearBtn = this.Controls.OfType<Button>().FirstOrDefault(b => b.Text != null && (b.Text.Equals("Clear", StringComparison.OrdinalIgnoreCase) || b.Text.Equals("Bersihkan", StringComparison.OrdinalIgnoreCase)));
            if (clearBtn != null)
            {
                clearBtn.Click -= button4_Click; // ensure not conflicting
                clearBtn.Click += (s, e) =>
                {
                    var log = GetControl<RichTextBox>("richTextBox1");
                    var input = GetControl<TextBox>("textBox4");
                    if (log != null) log.Clear();
                    if (input != null) input.Clear();
                    InitMistralSystemMessage();
                };
            }

            // Ensure Send button wires to button6_Click (already exists in designer)
        }

        private void InitMistralSystemMessage()
        {
            conversation.Clear();
            conversation.Add(new ChatMessage { role = "system", content = "Anda adalah AI yang ramah dan membantu." });
        }

        // CREATE
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                ComboBox jenisCombo;
                TextBox tbJumlah;
                ComboBox kabCombo;
                if (!GetInputControls(out jenisCombo, out tbJumlah, out kabCombo))
                {
                    MessageBox.Show("Field input tidak ditemukan di form");
                    return;
                }

                if (jenisCombo.SelectedItem == null || string.IsNullOrWhiteSpace(jenisCombo.SelectedItem.ToString()))
                {
                    MessageBox.Show("Pilih jenis sampah");
                    return;
                }

                if (!int.TryParse(tbJumlah.Text, out int jumlah))
                {
                    MessageBox.Show("Jumlah harus berupa angka");
                    return;
                }

                var lokasiValue = kabCombo != null ? kabCombo.SelectedItem?.ToString() : null;

                var s = new Sampah
                {
                    Nama = jenisCombo.SelectedItem.ToString(),
                    Jumlah = jumlah,
                    Lokasi = lokasiValue
                };

                collection.InsertOne(s);
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal membuat data: " + ex.Message);
            }
        }

        // UPDATE
        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                if (dataGridView1.CurrentRow == null) { MessageBox.Show("Pilih data dulu"); return; }
                var current = dataGridView1.CurrentRow.DataBoundItem as Sampah;
                if (current == null) return;

                ComboBox jenisCombo;
                TextBox tbJumlah;
                ComboBox kabCombo;
                if (!GetInputControls(out jenisCombo, out tbJumlah, out kabCombo))
                {
                    MessageBox.Show("Field input tidak ditemukan di form");
                    return;
                }

                if (jenisCombo.SelectedItem == null || string.IsNullOrWhiteSpace(jenisCombo.SelectedItem.ToString()))
                {
                    MessageBox.Show("Pilih jenis sampah");
                    return;
                }

                if (!int.TryParse(tbJumlah.Text, out int jumlah))
                {
                    MessageBox.Show("Jumlah harus berupa angka");
                    return;
                }

                var lokasiValue = kabCombo != null ? kabCombo.SelectedItem?.ToString() : null;

                var update = Builders<Sampah>.Update
                    .Set(x => x.Nama, jenisCombo.SelectedItem.ToString())
                    .Set(x => x.Jumlah, jumlah)
                    .Set(x => x.Lokasi, lokasiValue);

                collection.UpdateOne(x => x.Id == current.Id, update);
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal update data: " + ex.Message);
            }
        }

        // DELETE
        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                if (dataGridView1.CurrentRow == null) { MessageBox.Show("Pilih data dulu"); return; }
                var current = dataGridView1.CurrentRow.DataBoundItem as Sampah;
                if (current == null) return;

                var confirm = MessageBox.Show("Hapus item?", "Konfirmasi", MessageBoxButtons.YesNo);
                if (confirm != DialogResult.Yes) return;

                collection.DeleteOne(x => x.Id == current.Id);
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal delete data: " + ex.Message);
            }
        }

        // CLEAR / EDIT (button4) - clear inputs
        private void button4_Click(object sender, EventArgs e)
        {
            var jenisCombo = GetControl<ComboBox>("comboBox3");
            var tbJumlah = GetControl<TextBox>("textBox3");
            var kabCombo = FindKabupatenCombo();

            if (jenisCombo != null) jenisCombo.SelectedIndex = -1;
            if (tbJumlah != null) tbJumlah.Text = string.Empty;
            if (kabCombo != null) kabCombo.SelectedIndex = -1;
        }

        // EXPORT PDF (button5)
        private void button5_Click(object sender, EventArgs e)
        {
            try
            {
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Filter = "PDF files|*.pdf";
                    sfd.FileName = "data-sampah.pdf";
                    if (sfd.ShowDialog() != DialogResult.OK) return;

                    ExportToPdf(sfd.FileName);
                    MessageBox.Show("Export selesai");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal export PDF: " + ex.Message);
            }
        }

        private void ExportToPdf(string path)
        {
            // Requires iTextSharp NuGet package
            // Install-Package iTextSharp -Version 5.5.13.3
            var list = collection.Find(_ => true).ToList();

            using (var fs = new System.IO.FileStream(path, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None))
            {
                var doc = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 25, 25, 30, 30);
                var writer = iTextSharp.text.pdf.PdfWriter.GetInstance(doc, fs);
                doc.Open();

                var title = new iTextSharp.text.Paragraph("Data Sampah") { Alignment = iTextSharp.text.Element.ALIGN_CENTER };
                doc.Add(title);
                doc.Add(new iTextSharp.text.Paragraph("\n"));

                var table = new iTextSharp.text.pdf.PdfPTable(3) { WidthPercentage = 100 };
                table.AddCell("Nama");
                table.AddCell("Jumlah");
                table.AddCell("Lokasi");

                foreach (var it in list)
                {
                    table.AddCell(it.Nama ?? "");
                    table.AddCell(it.Jumlah.ToString());
                    table.AddCell(it.Lokasi ?? "");
                }

                doc.Add(table);
                doc.Close();
                writer.Close();
            }
        }

        private async void button6_Click(object sender, EventArgs e)
        {
            // find richTextBox1 (chat log) and textBox4 (input)
            var log = GetControl<RichTextBox>("richTextBox1");
            var input = GetControl<TextBox>("textBox4");
            if (log == null || input == null) return;

            var user = input.Text.Trim();
            if (string.IsNullOrEmpty(user)) return;

            log.AppendText("User: " + user + Environment.NewLine);

            // Try to find API key and model (use tagged combobox if present)
            var apiBox = FindApiKeyBox();

            var modelCombo = this.Controls.OfType<ComboBox>().FirstOrDefault(cb => cb.Tag != null && cb.Tag.ToString() == "model")
                             ?? GetControl<ComboBox>("comboBox1")
                             ?? GetControl<ComboBox>("comboBox2");

            input.Clear();

            if (apiBox != null && !string.IsNullOrWhiteSpace(apiBox.Text) && modelCombo != null && modelCombo.SelectedItem != null)
            {
                // Use Mistral API
                conversation.Add(new ChatMessage { role = "user", content = user });
                try
                {
                    string apiKey = apiBox.Text.Trim();
                    string model = modelCombo.SelectedItem.ToString();
                    string reply = await GetMistralResponse(apiKey, model).ConfigureAwait(false);
                    this.Invoke((Action)(() => log.AppendText("Bot(Mistral): " + reply + Environment.NewLine)));
                }
                catch (Exception ex)
                {
                    this.Invoke((Action)(() => log.AppendText("Bot: Error saat memanggil Mistral: " + ex.Message + Environment.NewLine)));
                }
            }
            else
            {
                // fallback to local simple chatbot
                var resp = GetChatbotResponse(user);
                log.AppendText("Bot: " + resp + Environment.NewLine);
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
                if (!res.IsSuccessStatusCode)
                    throw new Exception(responseText);

                dynamic result = JsonConvert.DeserializeObject(responseText);
                string reply = result.choices[0].message.content;

                conversation.Add(new ChatMessage { role = "assistant", content = reply });
                return reply;
            }
        }

        // local fallback simple chatbot
        private string GetChatbotResponse(string input)
        {
            input = input.ToLowerInvariant();
            if (input.Contains("halo") || input.Contains("hai"))
                return "Halo! Saya asisten data sampah Jabar.";

            if (input.Contains("jumlah") || input.Contains("total"))
            {
                var total = collection.CountDocuments(_ => true);
                return $"Total data sampah saat ini: {total}";
            }

            if (input.Contains("lokasi"))
            {
                var list = collection.Find(_ => true).ToList();
                if (list.Count == 0) return "Belum ada data.";
                var grp = list.GroupBy(x => x.Lokasi).OrderByDescending(g => g.Count()).First();
                return $"Lokasi terbanyak: {grp.Key} ({grp.Count()} records)";
            }

            return "Maaf, saya belum mengerti. Coba tanyakan jumlah atau lokasi.";
        }

        // Removed unused empty event handlers for clarity.
        // If designer references them, please remove the event wiring in the designer (__MainForm.Designer.cs__) as well.
        // This keeps the runtime clean and avoids dead code.

        // Helper to collect input controls used by create/update
        private bool GetInputControls(out ComboBox jenisCombo, out TextBox tbJumlah, out ComboBox kabCombo)
        {
            jenisCombo = GetControl<ComboBox>("comboBox3");
            tbJumlah = GetControl<TextBox>("textBox3");
            kabCombo = FindKabupatenCombo();
            return jenisCombo != null && tbJumlah != null;
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            // intentionally left blank — wired by designer
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            // intentionally left blank — wired by designer
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            // intentionally left blank — wired by designer
        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            // intentionally left blank — wired by designer
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            // intentionally left blank — wired by designer
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            // intentionally left blank — wired by designer
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            // intentionally left blank — wired by designer
        }

        private void button7_Click(object sender, EventArgs e)
        {
            // Clear button in designer — mimic previous Clear behaviour
            var log = this.Controls.Find("richTextBox1", true).FirstOrDefault() as RichTextBox;
            var input = this.Controls.Find("textBox4", true).FirstOrDefault() as TextBox;
            if (log != null) log.Clear();
            if (input != null) input.Clear();
            InitMistralSystemMessage();
        }
    }

    // ================= MODEL CHAT MESSAGE =================
    public class ChatMessage
    {
        public string role { get; set; } = string.Empty;
        public string content { get; set; } = string.Empty;
    }
}
