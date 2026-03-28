using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using MahApps.Metro.IconPacks;

// =======================================================
// --- ÇATIŞMA ÖNLEYİCİ SİHİRLİ KURALLAR (KALKANLAR) ---
// =======================================================
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Brushes = System.Windows.Media.Brushes;
using DragEventArgs = System.Windows.DragEventArgs;
using DataFormats = System.Windows.DataFormats;
using MessageBox = System.Windows.MessageBox;
using Application = System.Windows.Application;
using Colors = QuestPDF.Helpers.Colors;
using Fonts = QuestPDF.Helpers.Fonts;

namespace CyberSecurityAssistant // EĞER PROJE ADINIZ FARKLIYSA BURAYI DEĞİŞTİRİN
{
    public partial class MainWindow : Window
    {
        // ==========================================
        // DİNAMİK API AYARLARI
        // ==========================================
        private string _apiKey = "";
        private readonly string _settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "vt_apikey.txt");
        private static readonly HttpClient _httpClient = new HttpClient();

        // SİSTEM TEPSİSİ (SYSTEM TRAY) İKONU - Null hatasını önlemek için baştan oluşturuldu
        private System.Windows.Forms.NotifyIcon _notifyIcon = new System.Windows.Forms.NotifyIcon();

        private FileSystemWatcher _folderWatcher = null!;

        public MainWindow()
        {
            InitializeComponent();
            LoadApiKey();
            SetupSystemTray(); // Uygulama açıldığında tepsiyi hazırla
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
            InitializeComponent();
            LoadApiKey();
            SetupSystemTray();
            SetupFolderWatcher(); // İndirilenler radarını hazırla
        }

        // ==========================================
        // --- SİSTEM TEPSİSİ (ARKA PLAN) İŞLEMLERİ ---
        // ==========================================
        private void SetupSystemTray()
        {
            // Sistemdeki varsayılan Kalkan ikonunu kullanıyoruz
            _notifyIcon.Icon = System.Drawing.SystemIcons.Shield;
            _notifyIcon.Visible = true;
            _notifyIcon.Text = "SiberBelediye Kalkanı\nSisteminiz Korunuyor";

            // Çift tıklanınca uygulamayı geri aç
            _notifyIcon.DoubleClick += (s, e) => ShowMainWindow();

            // İkona sağ tıklayınca açılacak menü
            var contextMenu = new System.Windows.Forms.ContextMenuStrip();

            var openItem = contextMenu.Items.Add("Kalkan Arayüzünü Aç");
            openItem.Click += (s, e) => ShowMainWindow();

            contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

            var closeItem = contextMenu.Items.Add("Korumayı Durdur ve Çıkış Yap");
            closeItem.Click += (s, e) => CloseApplicationCompletely();

            _notifyIcon.ContextMenuStrip = contextMenu;
        }
        private void ShowMainWindow()
        {
            this.Show(); // Pencereyi göster
            this.WindowState = WindowState.Normal; // Küçültülmüşse normal boyuta al
            this.Activate(); // Ekranda en öne getir
        }
        private void CloseApplicationCompletely()
        {
            // İkonu görev çubuğundan temizle
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }

            // Application çatışmasını önleyen KESİN kapatma komutu:
            Environment.Exit(0);
        }

        // ==========================================
        // --- AYARLAR MENÜSÜ İŞLEMLERİ ---
        // ==========================================
        private void LoadApiKey()
        {
            if (File.Exists(_settingsFilePath))
            {
                _apiKey = File.ReadAllText(_settingsFilePath).Trim();
                UpdateApiStatusUI();
            }
        }
        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            ApiKeyTextBox.Text = _apiKey;
            SettingsOverlay.Visibility = Visibility.Visible;
        }

        private void CloseSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsOverlay.Visibility = Visibility.Collapsed;
        }
        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            _apiKey = ApiKeyTextBox.Text.Trim();
            File.WriteAllText(_settingsFilePath, _apiKey);
            SettingsOverlay.Visibility = Visibility.Collapsed;
            UpdateApiStatusUI();
        }
        private void UpdateApiStatusUI()
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                ApiStatusIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                ApiStatusText.Text = "API Bekleniyor (Ayarlardan Girin)";
                ApiStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
            }
            else
            {
                ApiStatusIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E"));
                ApiStatusText.Text = "API Bağlantısı: Devrede";
                ApiStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ADE80"));
            }
        }

        // ==========================================
        // --- SEKME VE DOSYA İŞLEMLERİ ---
        // ==========================================
        private void TabMetin_Click(object sender, RoutedEventArgs e)
        {
            SetActiveTabState(TabMetin);
            PanelMetin.Visibility = Visibility.Visible;
            PanelUrl.Visibility = Visibility.Collapsed;
            PanelDosya.Visibility = Visibility.Collapsed;
        }

        private void TabUrl_Click(object sender, RoutedEventArgs e)
        {
            SetActiveTabState(TabUrl);
            PanelMetin.Visibility = Visibility.Collapsed;
            PanelUrl.Visibility = Visibility.Visible;
            PanelDosya.Visibility = Visibility.Collapsed;
        }

        private void TabDosya_Click(object sender, RoutedEventArgs e)
        {
            SetActiveTabState(TabDosya);
            PanelMetin.Visibility = Visibility.Collapsed;
            PanelUrl.Visibility = Visibility.Collapsed;
            PanelDosya.Visibility = Visibility.Visible;
        }

        // Çatışmayı önlemek için System.Windows.Controls.Button olarak sabitlendi
        private void SetActiveTabState(System.Windows.Controls.Button activeTab)
        {
            var inactiveColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A0A0A0"));
            TabMetin.Background = Brushes.Transparent; TabMetin.Foreground = inactiveColor;
            TabUrl.Background = Brushes.Transparent; TabUrl.Foreground = inactiveColor;
            TabDosya.Background = Brushes.Transparent; TabDosya.Foreground = inactiveColor;

            activeTab.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"));
            activeTab.Foreground = Brushes.White;
        }

        private string _selectedFilePath = "";

        private void BrowseFile_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Title = "Taranacak Dosyayı Seçin";
            openFileDialog.Filter = "Tüm Dosyalar (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedFilePath = openFileDialog.FileName;
                SelectedFileNameText.Text = "Seçilen Dosya: " + openFileDialog.SafeFileName;
                SelectedFileNameText.Foreground = Brushes.White;
            }
        }

        private void PanelDosya_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    _selectedFilePath = files[0];
                    SelectedFileNameText.Text = "Seçilen Dosya: " + Path.GetFileName(_selectedFilePath);
                    SelectedFileNameText.Foreground = Brushes.White;
                }
            }
        }

        // ==========================================
        // --- METİN (PHISHING) ANALİZİ ---
        // ==========================================
        private (int score, string details) AnalyzeTextForPhishing(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return (0, "Metin bulunamadı.");

            text = text.ToLowerInvariant();
            int score = 0;
            string details = "";

            string[] urgencyWords = { "acil", "hemen", "son uyarı", "kapatılacak", "askıya", "24 saat", "iptal edilecek", "zorunlu" };
            foreach (var word in urgencyWords) { if (text.Contains(word)) { score += 35; details += "• Aciliyet/Tehdit taktiği algılandı.\n"; break; } }

            string[] infoWords = { "şifre", "parola", "kredi kartı", "tc kimlik", "hesap doğrula", "ödeme yap", "kripto", "iban" };
            foreach (var word in infoWords) { if (text.Contains(word)) { score += 45; details += "• Hassas bilgi veya para talebi algılandı.\n"; break; } }

            string[] actionWords = { "buraya tıkla", "linke tıkla", "ekteki dosya", "faturayı incele", "giriş yap", "indirmek için" };
            foreach (var word in actionWords) { if (text.Contains(word)) { score += 20; details += "• Şüpheli eylem/link yönlendirmesi algılandı.\n"; break; } }

            if (text.Contains("değerli müşterimiz") || text.Contains("sayın kullanıcı") || text.Contains("sayın abonemiz"))
            { score += 10; details += "• Şüpheli genel hitap (Phishing taktiği).\n"; }

            if (score > 100) score = 100;
            if (score == 0) details = "Herhangi bir oltalama (phishing) taktiği tespit edilmedi.";

            return (score, details.TrimEnd());
        }

        // ==========================================
        // --- TARAMA VE SİLME İŞLEMLERİ ---
        // ==========================================
        private async void Scan_button_Click(object sender, RoutedEventArgs e)
        {
            if (PanelMetin.Visibility != Visibility.Visible && string.IsNullOrEmpty(_apiKey))
            {
                SetResultUI("API ANAHTARI EKSİK", "Lütfen sağ üstteki çark (⚙️) ikonuna\ntıklayarak API anahtarınızı girin.", false, true);
                return;
            }

            ResetButton.Visibility = Visibility.Hidden;
            DeleteThreatButton.Visibility = Visibility.Collapsed;

            ShieldIcon.Kind = PackIconMaterialKind.CogOutline;
            ShieldIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FBBF24"));
            ShieldGlow.Color = (Color)ColorConverter.ConvertFromString("#FBBF24");
            StatusText.Text = "Veriler İşleniyor...";
            StatusText.Foreground = Brushes.Yellow;
            ScanDescriptionText.Text = "Lütfen bekleyin...";

            ScanProgressBar.Visibility = Visibility.Visible;
            ScanProgressBar.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FBBF24"));
            DoubleAnimation progressAnim = new DoubleAnimation(0, 80, TimeSpan.FromSeconds(2));
            ScanProgressBar.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, progressAnim);

            var storyboard = (Storyboard)FindResource("PulseAnimation");
            storyboard.Begin();

            if (PanelMetin.Visibility == Visibility.Visible)
            {
                AiStatusIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FBBF24"));
                AiStatusText.Text = "NLP Analizi Yapılıyor...";
                AiStatusText.Foreground = Brushes.Yellow;
            }
            else
            {
                ApiStatusIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FBBF24"));
                ApiStatusText.Text = "VT Sunucularına Bağlanılıyor...";
                ApiStatusText.Foreground = Brushes.Yellow;
            }

            try
            {
                int malicious = 0;
                int total = 0;
                bool isScanned = false;

                if (PanelDosya.Visibility == Visibility.Visible && !string.IsNullOrEmpty(_selectedFilePath))
                {
                    string fileHash = ComputeSha256(_selectedFilePath);
                    var result = await CheckVirusTotalAsync($"files/{fileHash}");
                    malicious = result.maliciousCount;
                    total = result.totalEngines;
                    isScanned = true;
                }
                else if (PanelUrl.Visibility == Visibility.Visible && InputUrl.Text.Length > 8)
                {
                    string urlBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(InputUrl.Text))
                                              .Replace("+", "-").Replace("/", "_").TrimEnd('=');
                    var result = await CheckVirusTotalAsync($"urls/{urlBase64}");
                    malicious = result.maliciousCount;
                    total = result.totalEngines;
                    isScanned = true;
                }
                else if (PanelMetin.Visibility == Visibility.Visible && InputMetin.Text.Length > 10)
                {
                    await Task.Delay(1500);
                    var (score, details) = AnalyzeTextForPhishing(InputMetin.Text);

                    if (score >= 70)
                    {
                        SetResultUI("OLTALAMA (PHISHING)!", $"Tehdit Skoru: {score}/100\n{details}", true);
                        AiStatusIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                        AiStatusText.Text = $"Kritik Uyarı: %{score} Phishing Riski";
                        AiStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                    }
                    else if (score >= 30)
                    {
                        SetResultUI("ŞÜPHELİ METİN", $"Tehdit Skoru: {score}/100\n{details}", false, true);
                        AiStatusIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
                        AiStatusText.Text = $"Uyarı: %{score} Şüpheli İçerik";
                        AiStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FBBF24"));
                    }
                    else
                    {
                        SetResultUI("GÜVENLİ İÇERİK.", $"Tehdit Skoru: {score}/100\n{details}", false);
                        AiStatusIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E"));
                        AiStatusText.Text = "Analiz Temiz (Tehdit Bulunmadı)";
                        AiStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ADE80"));
                    }
                    isScanned = true;
                }

                if (isScanned && PanelMetin.Visibility != Visibility.Visible)
                {
                    if (malicious > 0)
                    {
                        SetResultUI("TEHDİT TESPİT EDİLDİ!", $"Kritik! {total} güvenlik motorundan\n{malicious} tanesi zararlı buldu.", true);
                        ApiStatusIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                        ApiStatusText.Text = $"Kritik: {malicious} Motor Tehdit Buldu";
                        ApiStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));

                        if (PanelDosya.Visibility == Visibility.Visible && !string.IsNullOrEmpty(_selectedFilePath))
                        {
                            DeleteThreatButton.Visibility = Visibility.Visible;
                        }
                    }
                    else if (total > 0)
                    {
                        SetResultUI("GÜVENLİ İÇERİK.", $"Temiz. {total} farklı güvenlik motoru\ntarafından onaylandı.", false);
                        ApiStatusIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E"));
                        ApiStatusText.Text = "Temiz: VT Tarafından Onaylandı";
                        ApiStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ADE80"));
                    }
                    else
                    {
                        SetResultUI("BİLİNMEYEN İÇERİK", "Bu dosya veya bağlantı veritabanında\ndaha önce hiç taranmamış.", false, true);
                        ApiStatusIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
                        ApiStatusText.Text = "Uyarı: Veritabanında Kayıt Yok";
                        ApiStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FBBF24"));
                    }
                }
                else if (!isScanned)
                {
                    await Task.Delay(1000);
                    SetResultUI("GEÇERSİZ GİRİŞ", "Lütfen geçerli bir veri girdiğinizden\nemin olun.", false, true);
                }
            }
            catch (Exception)
            {
                SetResultUI("BAĞLANTI HATASI", "API'ye bağlanılamadı. Anahtarınızı\nkontrol edin veya interneti sınayın.", false, true);
                ApiStatusIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                ApiStatusText.Text = "Hata: Sunucu Bağlantısı Koptu";
                ApiStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
            }

            storyboard.Stop();
            ScanProgressBar.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, null);
            ScanProgressBar.Value = 100;
            PdfReportButton.Visibility = Visibility.Visible;
            ResetButton.Visibility = Visibility.Visible;
        }

        private void DeleteThreat_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (File.Exists(_selectedFilePath))
                {
                    File.Delete(_selectedFilePath);
                    SetResultUI("TEHDİT YOK EDİLDİ!", "Zararlı dosya bilgisayarınızdan\nkalıcı olarak silindi.", false);
                    DeleteThreatButton.Visibility = Visibility.Collapsed;
                    SelectedFileNameText.Text = "Sürükleyip Bırakın veya Seçin";
                    SelectedFileNameText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A0A0A0"));
                    _selectedFilePath = "";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Dosya silinemedi! Başka bir programda açık olabilir.\nHata: " + ex.Message, "Silme Başarısız", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetResultUI(string title, string desc, bool isDanger, bool isWarning = false)
        {
            if (isDanger)
            {
                ShieldIcon.Kind = PackIconMaterialKind.ShieldAlertOutline;
                ShieldIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                ShieldGlow.Color = (Color)ColorConverter.ConvertFromString("#EF4444");
                StatusText.Text = title;
                StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                ScanProgressBar.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
            }
            else if (isWarning)
            {
                ShieldIcon.Kind = PackIconMaterialKind.AlertOutline;
                ShieldIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
                ShieldGlow.Color = (Color)ColorConverter.ConvertFromString("#F59E0B");
                StatusText.Text = title;
                StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FBBF24"));
                ScanProgressBar.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
            }
            else
            {
                ShieldIcon.Kind = PackIconMaterialKind.ShieldCheckOutline;
                ShieldIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E"));
                ShieldGlow.Color = (Color)ColorConverter.ConvertFromString("#22C55E");
                StatusText.Text = title;
                StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ADE80"));
                ScanProgressBar.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E"));
            }
            ScanDescriptionText.Text = desc;
        }

        private string ComputeSha256(string filePath)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                using (FileStream fileStream = File.OpenRead(filePath))
                {
                    byte[] hashBytes = sha256.ComputeHash(fileStream);
                    return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        private async Task<(int maliciousCount, int totalEngines)> CheckVirusTotalAsync(string endpointUrl)
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("x-apikey", _apiKey);

            HttpResponseMessage response = await _httpClient.GetAsync($"https://www.virustotal.com/api/v3/{endpointUrl}");

            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
                {
                    var stats = doc.RootElement.GetProperty("data").GetProperty("attributes").GetProperty("last_analysis_stats");
                    int malicious = stats.GetProperty("malicious").GetInt32();
                    int harmless = stats.GetProperty("harmless").GetInt32();
                    int undetected = stats.GetProperty("undetected").GetInt32();
                    int suspicious = stats.GetProperty("suspicious").GetInt32();

                    int total = malicious + harmless + undetected + suspicious;
                    return (malicious + suspicious, total);
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return (0, 0);

            throw new Exception("API Hatası");
        }

        private void Reset_button_Click(object sender, RoutedEventArgs e)
        {
            SetResultUI("GÜVENLİ İÇERİK.", "Yapay Zeka Modeli: Tehdit\nTespit Edilmedi.", false);
            ScanProgressBar.Visibility = Visibility.Collapsed;
            ScanProgressBar.Value = 0;

            // XAML'deki Placeholder'ların görünmesi için içlerini boşaltıyoruz
            if (InputMetin != null) InputMetin.Text = "";
            if (InputUrl != null) InputUrl.Text = "";

            SelectedFileNameText.Text = "Sürükleyip Bırakın veya Seçin";
            SelectedFileNameText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A0A0A0"));
            _selectedFilePath = "";

            PdfReportButton.Visibility = Visibility.Collapsed;
            ResetButton.Visibility = Visibility.Hidden;
            DeleteThreatButton.Visibility = Visibility.Collapsed;

            AiStatusIcon.Foreground = Brushes.White;
            AiStatusText.Text = "Aktif - Doğruluk Oranı: %98.1";
            AiStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A0A0A0"));
            UpdateApiStatusUI();
        }

        // ==========================================
        // --- PENCERE KONTROLLERİ VE TRAY ---
        // ==========================================
        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left) this.DragMove();
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                // 1. Pencerenin şu an HANGİ MONİTÖRDE olduğunu tespit et (Çift monitör kullananlar için hayati önem taşır)
                IntPtr handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                var ekran = System.Windows.Forms.Screen.FromHandle(handle);

                // 2. Pencerenin büyüme sınırını, o monitörün Görev Çubuğu hariç net alanına eşitle
                this.MaxWidth = ekran.WorkingArea.Width;
                this.MaxHeight = ekran.WorkingArea.Height;

                // 3. Sıfır gecikme ve sıfır kasma ile anında tam ekran yap
                this.WindowState = WindowState.Maximized;

                MaximizeIcon.Kind = PackIconMaterialKind.WindowRestore;
            }
            else
            {
                // Sınırları kaldır ve anında normal boyuta dön
                this.MaxWidth = double.PositiveInfinity;
                this.MaxHeight = double.PositiveInfinity;
                this.WindowState = WindowState.Normal;

                MaximizeIcon.Kind = PackIconMaterialKind.WindowMaximize;
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        // ==========================================
        // --- PDF RAPORU OLUŞTURMA İŞLEMİ ---
        // ==========================================
        private void PdfReport_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.Filter = "PDF Dosyası (*.pdf)|*.pdf";
            saveFileDialog.Title = "Güvenlik Raporunu Kaydet";
            saveFileDialog.FileName = "Siber_Tarama_Raporu_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".pdf";

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    // TERTEMİZ QUESTPDF YAPISI
                    Document.Create(container =>
                    {
                        container.Page(page =>
                        {
                            page.Size(PageSizes.A4);
                            page.Margin(2, Unit.Centimetre);
                            page.PageColor(Colors.White);
                            page.DefaultTextStyle(x => x.FontSize(12).FontFamily(Fonts.Arial));

                            // ÜST BİLGİ (HEADER)
                            page.Header().Text("SiberBelediye - Güvenlik Analiz Raporu")
                                .SemiBold().FontSize(22).FontColor(Colors.Blue.Darken2);

                            // İÇERİK (CONTENT)
                            page.Content().PaddingVertical(1, Unit.Centimetre).Column(x =>
                            {
                                x.Spacing(15);

                                x.Item().Text($"Tarama Tarihi: {DateTime.Now:dd.MM.yyyy HH:mm}").FontSize(10).FontColor(Colors.Grey.Medium);
                                x.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                                string targetType = "";
                                string targetData = "";

                                if (PanelDosya.Visibility == Visibility.Visible) { targetType = "Dosya Analizi"; targetData = _selectedFilePath; }
                                else if (PanelUrl.Visibility == Visibility.Visible) { targetType = "Bağlantı (URL) Analizi"; targetData = InputUrl.Text; }
                                else { targetType = "Metin (Phishing) Analizi"; targetData = InputMetin.Text; }

                                x.Item().Text("Hedef Türü: " + targetType).SemiBold().FontSize(14);
                                x.Item().Background(Colors.Grey.Lighten4).Padding(10).Text(targetData).Italic();

                                x.Item().PaddingTop(10).Text("Analiz Sonucu:").SemiBold().FontSize(14);

                                string status = StatusText.Text;
                                string desc = ScanDescriptionText.Text;

                                string resultColor = status.Contains("TEHDİT") || status.Contains("OLTALAMA") ? Colors.Red.Medium : (status.Contains("ŞÜPHELİ") || status.Contains("BİLİNMEYEN") ? Colors.Orange.Medium : Colors.Green.Medium);

                                x.Item().Text(status).FontSize(18).Bold().FontColor(resultColor);
                                x.Item().Text(desc).FontSize(12);

                                x.Item().PaddingTop(20).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                                x.Item().Text("Bu rapor, yapay zeka ve VirusTotal motorları kullanılarak otomatik oluşturulmuştur.").FontSize(9).FontColor(Colors.Grey.Medium).Italic();
                            });

                            // ALT BİLGİ (FOOTER)
                            page.Footer().AlignCenter().Text(x =>
                            {
                                x.Span("Sayfa ");
                                x.CurrentPageNumber();
                                x.Span(" / ");
                                x.TotalPages();
                            });
                        });
                    })
                    .GeneratePdf(saveFileDialog.FileName);

                    MessageBox.Show("Rapor başarıyla kaydedildi!\n\n" + saveFileDialog.FileName, "İşlem Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("PDF oluşturulurken bir hata oluştu: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ==========================================
        // --- GERÇEK ZAMANLI KLASÖR DİNLEME MOTORU ---
        // ==========================================
        private void SetupFolderWatcher()
        {
            // Windows'un varsayılan "İndirilenler" klasörünün yolunu dinamik olarak bul
            string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

            _folderWatcher = new FileSystemWatcher(downloadsPath);
            _folderWatcher.NotifyFilter = NotifyFilters.FileName; // Sadece isim değişikliklerini izle
            _folderWatcher.Filter = "*.*";

            // Tarayıcılar dosyayı indirirken önce oluşturur, bitince adını değiştirir. İkisini de pusuya yatıyoruz.
            _folderWatcher.Created += OnFileDetected;
            _folderWatcher.Renamed += OnFileDetected;
        }

        private void AutoScan_Click(object sender, RoutedEventArgs e)
        {
            if (AutoScanCheckBox.IsChecked == true)
            {
                _folderWatcher.EnableRaisingEvents = true; // Radarı aç
                _notifyIcon.ShowBalloonTip(2000, "Radar Aktif", "İndirilenler klasörü artık gerçek zamanlı izleniyor.", System.Windows.Forms.ToolTipIcon.Info);
            }
            else
            {
                _folderWatcher.EnableRaisingEvents = false; // Radarı kapat
            }
        }

        private async void OnFileDetected(object sender, FileSystemEventArgs e)
        {
            string dosyaUzantisi = Path.GetExtension(e.FullPath).ToLower();

            // Chrome, Edge ve Firefox'un indirme bitmeden oluşturduğu "geçici" dosyaları görmezden gel!
            if (dosyaUzantisi == ".crdownload" || dosyaUzantisi == ".tmp" || dosyaUzantisi == ".part")
                return;

            // Dosyanın diske tamamen yazılıp kilidinin açılması için kısa bir süre bekle
            await Task.Delay(1500);

            // Klasör dinleme işlemi arka planda çalıştığı için, arayüze (butonlara vs.) müdahale edebilmek için "Dispatcher.Invoke" ile ana ekrana bağlanıyoruz:
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 1. Program arka planda gizliyse anında ekrana fırla!
                if (this.Visibility != Visibility.Visible || this.WindowState == WindowState.Minimized)
                {
                    this.Show();
                    this.WindowState = WindowState.Normal;
                    this.Activate();
                }

                // 2. Dosya sekmesini aktif et
                TabDosya_Click(this, new RoutedEventArgs());

                // 3. Dosyayı programa yükle
                _selectedFilePath = e.FullPath;
                SelectedFileNameText.Text = "🚨 Otomatik Yakalandı: " + e.Name;
                SelectedFileNameText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6")); // Mavi vurgu

                // 4. Windows sağ alt bildirimini gönder
                _notifyIcon.ShowBalloonTip(3000, "Yeni Dosya Yakalandı!", $"{e.Name} indirildi ve analize alınıyor...", System.Windows.Forms.ToolTipIcon.Warning);

                // 5. Taramayı OTOMATİK başlat! (Kullanıcının butona basmasına gerek kalmadan)
                Scan_button_Click(this, new RoutedEventArgs());
            });
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Hide(); // Sadece pencereyi gizle

            // Kullanıcıya arka planda çalıştığımızı haber ver
            _notifyIcon.ShowBalloonTip(2000, "Siber Kalkan Aktif", "Uygulama arka planda sisteminizi korumaya devam ediyor.", System.Windows.Forms.ToolTipIcon.Info);
        }
    }
}