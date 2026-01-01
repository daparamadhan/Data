using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Datas.Models;
using Datas.Services;
using System.Security;

namespace Datas
{
    public partial class DataSampahForm : Form
    {
        private Services.MongoService _mongo;
        private BindingList<Sampah> _items = new BindingList<Sampah>();

        // UI controls created in code-behind for quick setup
        private DataGridView dgv;
        private TextBox txtKabupaten, txtJenis, txtBerat, txtTanggal, txtChat, txtChatLog;
        private Button btnCreate, btnUpdate, btnDelete, btnRefresh, btnExportPdf, btnSendChat, btnChatMistral;

        public DataSampahForm()
        {
            InitializeComponent();

            // Initialize MongoService (update connection string as needed)
            // For local MongoDB default: "mongodb://localhost:27017"
            _mongo = new MongoService("mongodb://localhost:27017");

            InitializeCustomControls();
            // use async load
            _ = LoadDataAsync();
        }

        private void InitializeCustomControls()
        {
            this.Text = "CRUD Data Sampah - Jawa Barat";
            this.Width = 1000;
            this.Height = 700;

            dgv = new DataGridView { Left = 10, Top = 10, Width = 650, Height = 600, AutoGenerateColumns = false };
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.MultiSelect = false;

            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Id", DataPropertyName = "Id", Width = 150 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Kabupaten", DataPropertyName = "Kabupaten", Width = 120 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Jenis", DataPropertyName = "Jenis", Width = 120 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Berat (kg)", DataPropertyName = "Berat", Width = 80 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tanggal", DataPropertyName = "Tanggal", Width = 150 });

            this.Controls.Add(dgv);

            var lblKab = new Label { Left = 670, Top = 10, Text = "Kabupaten" };
            txtKabupaten = new TextBox { Left = 670, Top = 30, Width = 300 };

            var lblJenis = new Label { Left = 670, Top = 60, Text = "Jenis" };
            txtJenis = new TextBox { Left = 670, Top = 80, Width = 300 };

            var lblBerat = new Label { Left = 670, Top = 110, Text = "Berat (kg)" };
            txtBerat = new TextBox { Left = 670, Top = 130, Width = 300 };

            var lblTanggal = new Label { Left = 670, Top = 160, Text = "Tanggal (yyyy-MM-dd)" };
            txtTanggal = new TextBox { Left = 670, Top = 180, Width = 300 };

            btnCreate = new Button { Left = 670, Top = 220, Width = 90, Text = "Create" };
            btnUpdate = new Button { Left = 770, Top = 220, Width = 90, Text = "Update" };
            btnDelete = new Button { Left = 870, Top = 220, Width = 90, Text = "Delete" };
            btnRefresh = new Button { Left = 670, Top = 260, Width = 90, Text = "Refresh" };
            btnExportPdf = new Button { Left = 770, Top = 260, Width = 190, Text = "Export to PDF" };

            this.Controls.Add(lblKab);
            this.Controls.Add(txtKabupaten);
            this.Controls.Add(lblJenis);
            this.Controls.Add(txtJenis);
            this.Controls.Add(lblBerat);
            this.Controls.Add(txtBerat);
            this.Controls.Add(lblTanggal);
            this.Controls.Add(txtTanggal);
            this.Controls.Add(btnCreate);
            this.Controls.Add(btnUpdate);
            this.Controls.Add(btnDelete);
            this.Controls.Add(btnRefresh);
            this.Controls.Add(btnExportPdf);

            // Chatbot UI
            var lblChat = new Label { Left = 10, Top = 620, Text = "Chatbot (Simple)" };
            txtChatLog = new TextBox { Left = 10, Top = 640, Width = 650, Height = 140, Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true };
            txtChat = new TextBox { Left = 10, Top = 790, Width = 550, Height = 25 };
            btnSendChat = new Button { Left = 570, Top = 790, Width = 90, Text = "Send" };
            btnChatMistral = new Button { Left = 670, Top = 790, Width = 150, Text = "Send via Mistral" };

            // Adjust form height to fit chat UI
            this.Height = 880;

            this.Controls.Add(lblChat);
            this.Controls.Add(txtChatLog);
            this.Controls.Add(txtChat);
            this.Controls.Add(btnSendChat);
            this.Controls.Add(btnChatMistral);

            // Events
            btnCreate.Click += BtnCreate_Click;
            btnUpdate.Click += BtnUpdate_Click;
            btnDelete.Click += BtnDelete_Click;
            btnRefresh.Click += BtnRefresh_Click;
            btnExportPdf.Click += BtnExportPdf_Click;
            btnSendChat.Click += BtnSendChat_Click;
            btnChatMistral.Click += BtnChatMistral_Click;

            dgv.SelectionChanged += Dgv_SelectionChanged;

            dgv.DataSource = _items;
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var list = await _mongo.GetAllAsync().ConfigureAwait(false);
                // switch back to UI thread to update binding list
                this.Invoke((Action)(() =>
                {
                    _items.Clear();
                    foreach (var it in list)
                        _items.Add(it);
                }));
            }
            catch (Exception ex)
            {
                this.Invoke((Action)(() => MessageBox.Show("Gagal load data: " + ex.Message)));
            }
        }

        private void Dgv_SelectionChanged(object sender, EventArgs e)
        {
            if (dgv.CurrentRow == null) return;
            var item = dgv.CurrentRow.DataBoundItem as Sampah;
            if (item == null) return;

            txtKabupaten.Text = item.Kabupaten;
            txtJenis.Text = item.Jenis;
            txtBerat.Text = item.Berat.ToString();
            txtTanggal.Text = item.Tanggal.ToString("yyyy-MM-dd");
        }

        private async void BtnCreate_Click(object sender, EventArgs e)
        {
            if (!double.TryParse(txtBerat.Text, out double berat))
            {
                MessageBox.Show("Berat harus angka");
                return;
            }
            if (!DateTime.TryParse(txtTanggal.Text, out DateTime tanggal))
            {
                MessageBox.Show("Tanggal tidak valid");
                return;
            }

            var s = new Sampah
            {
                Kabupaten = txtKabupaten.Text,
                Jenis = txtJenis.Text,
                Berat = berat,
                Tanggal = tanggal
            };

            try
            {
                await _mongo.CreateAsync(s).ConfigureAwait(false);
                await LoadDataAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.Invoke((Action)(() => MessageBox.Show("Gagal membuat data: " + ex.Message)));
            }
        }

        private async void BtnUpdate_Click(object sender, EventArgs e)
        {
            if (dgv.CurrentRow == null) return;
            var item = dgv.CurrentRow.DataBoundItem as Sampah;
            if (item == null) return;

            if (!double.TryParse(txtBerat.Text, out double berat))
            {
                MessageBox.Show("Berat harus angka");
                return;
            }
            if (!DateTime.TryParse(txtTanggal.Text, out DateTime tanggal))
            {
                MessageBox.Show("Tanggal tidak valid");
                return;
            }

            item.Kabupaten = txtKabupaten.Text;
            item.Jenis = txtJenis.Text;
            item.Berat = berat;
            item.Tanggal = tanggal;

            try
            {
                await _mongo.UpdateAsync(item).ConfigureAwait(false);
                await LoadDataAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.Invoke((Action)(() => MessageBox.Show("Gagal update data: " + ex.Message)));
            }
        }

        private async void BtnDelete_Click(object sender, EventArgs e)
        {
            if (dgv.CurrentRow == null) return;
            var item = dgv.CurrentRow.DataBoundItem as Sampah;
            if (item == null) return;

            var confirm = MessageBox.Show("Hapus item?", "Konfirmasi", MessageBoxButtons.YesNo);
            if (confirm != DialogResult.Yes) return;

            try
            {
                await _mongo.DeleteAsync(item.Id).ConfigureAwait(false);
                await LoadDataAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.Invoke((Action)(() => MessageBox.Show("Gagal delete data: " + ex.Message)));
            }
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            _ = LoadDataAsync();
        }

        private void BtnExportPdf_Click(object sender, EventArgs e)
        {
            // Simple PDF export using iTextSharp. Requires iTextSharp nuget: iTextSharp (older) or iText7.
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
            // Use iTextSharp.text and iTextSharp.text.pdf
            // Example uses iTextSharp 5.x API
            // Install-Package iTextSharp -Version 5.5.13.3
            try
            {
                var list = _mongo.GetAll();
                using (var fs = new System.IO.FileStream(path, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None))
                {
                    var doc = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 25, 25, 30, 30);
                    var writer = iTextSharp.text.pdf.PdfWriter.GetInstance(doc, fs);
                    doc.Open();

                    var title = new iTextSharp.text.Paragraph("Data Sampah - Jawa Barat") { Alignment = iTextSharp.text.Element.ALIGN_CENTER };
                    doc.Add(title);
                    doc.Add(new iTextSharp.text.Paragraph("\n"));

                    var table = new iTextSharp.text.pdf.PdfPTable(4) { WidthPercentage = 100 };
                    table.AddCell("Kabupaten");
                    table.AddCell("Jenis");
                    table.AddCell("Berat (kg)");
                    table.AddCell("Tanggal");

                    foreach (var it in list)
                    {
                        table.AddCell(it.Kabupaten ?? "");
                        table.AddCell(it.Jenis ?? "");
                        table.AddCell(it.Berat.ToString());
                        table.AddCell(it.Tanggal.ToString("yyyy-MM-dd"));
                    }

                    doc.Add(table);
                    doc.Close();
                    writer.Close();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("PDF export error: " + ex.Message, ex);
            }
        }

        private async void BtnChatMistral_Click(object sender, EventArgs e)
        {
            var input = txtChat.Text.Trim();
            if (string.IsNullOrEmpty(input)) return;

            AppendChat("User: " + input);

            // Read API key from environment variable MISTRAL_API_KEY
            var secure = ApiKeyProvider.GetSecureApiKey("MISTRAL_API_KEY");
            if (secure == null)
            {
                AppendChat("Bot: API key MISTRAL_API_KEY tidak ditemukan. Set environment variable terlebih dahulu.");
                return;
            }

            // Convert secure string to normal string just for HTTP call; clear asap.
            string apiKey = ApiKeyProvider.SecureStringToString(secure);

            try
            {
                // Example HTTP call to Mistral (user must implement actual endpoint/client according to Mistral docs)
                string resp = await SendToMistralAsync(apiKey, input).ConfigureAwait(false);
                this.Invoke((Action)(() => AppendChat("Bot(Mistral): " + resp)));
            }
            catch (Exception ex)
            {
                this.Invoke((Action)(() => AppendChat("Bot: Error saat memanggil Mistral: " + ex.Message)));
            }
            finally
            {
                // zero out the apiKey variable
                if (apiKey != null)
                {
                    var charArr = apiKey.ToCharArray();
                    for (int i = 0; i < charArr.Length; i++) charArr[i] = '\0';
                    apiKey = null;
                }
            }

            txtChat.Text = string.Empty;
        }

        private async Task<string> SendToMistralAsync(string apiKey, string prompt)
        {
            // Minimal example using HttpClient. Replace endpoint and payload per Mistral API.
            using (var http = new System.Net.Http.HttpClient())
            {
                http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                var payload = new
                {
                    model = "JABAR",
                    input = prompt
                };
                var content = new System.Net.Http.StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var res = await http.PostAsync("https://api.mistral.ai/v1/generate", content).ConfigureAwait(false);
                res.EnsureSuccessStatusCode();
                var text = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
                // naive parsing; user can update according to response schema
                return text;
            }
        }

        private void BtnSendChat_Click(object sender, EventArgs e)
        {
            var input = txtChat.Text.Trim();
            if (string.IsNullOrEmpty(input)) return;
            AppendChat("User: " + input);

            var response = GetChatbotResponse(input);
            AppendChat("Bot: " + response);
            txtChat.Text = string.Empty;
        }

        private void AppendChat(string text)
        {
            txtChatLog.AppendText(text + Environment.NewLine);
        }

        private string GetChatbotResponse(string input)
        {
            // Very simple rule-based chatbot focused on sampah in Jawa Barat
            input = input.ToLowerInvariant();
            if (input.Contains("halo") || input.Contains("hai") || input.Contains("hello"))
                return "Halo! Saya asisten data sampah. Anda bisa bertanya tentang jumlah data, export, atau statistik sederhana.";

            if (input.Contains("jumlah") || input.Contains("total"))
            {
                var total = _mongo.GetAll().Count;
                return $"Total data sampah saat ini: {total}";
            }

            if (input.Contains("kabupaten") && input.Contains("paling"))
            {
                // simple most common by kabupaten
                var list = _mongo.GetAll();
                if (list.Count == 0) return "Belum ada data.";
                var grp = list.GroupBy(x => x.Kabupaten).OrderByDescending(g => g.Count()).First();
                return $"Kabupaten dengan data terbanyak: {grp.Key} ({grp.Count()} records)";
            }

            if (input.Contains("export"))
            {
                return "Untuk export ke PDF, klik tombol 'Export to PDF' di aplikasi.";
            }

            return "Maaf, saya belum mengerti. Coba tanya jumlah, kabupaten paling, atau export.";
        }
    }
}
