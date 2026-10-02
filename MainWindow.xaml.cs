using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace oscdimg_wrapprt_gui
{
    public partial class MainWindow : Window
    {
        private CancellationTokenSource? _runCts;

        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnClosed(EventArgs e)
        {
            _runCts?.Cancel();
            base.OnClosed(e);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SourceTextBox.TextChanged += OnOptionChanged;
            LabelTextBox.TextChanged += OnOptionChanged;
            DestinationTextBox.TextChanged += OnOptionChanged;
            FormatCombo.SelectionChanged += OnOptionChanged;
            IsoDosRadio.Checked += OnOptionChanged;
            IsoLongRadio.Checked += OnOptionChanged;
            IsoNtRadio.Checked += OnOptionChanged;
            IsoLowerCheck.Checked += OnOptionChanged;
            IsoLowerCheck.Unchecked += OnOptionChanged;
            JolietBothRadio.Checked += OnOptionChanged;
            JolietOnlyRadio.Checked += OnOptionChanged;
            JolietReadmeTextBox.TextChanged += OnOptionChanged;
            UdfBothRadio.Checked += OnOptionChanged;
            UdfOnlyRadio.Checked += OnOptionChanged;
            UdfVersionCombo.SelectionChanged += OnOptionChanged;
            UdfEmbedDataCheck.Checked += OnOptionChanged;
            UdfEmbedDataCheck.Unchecked += OnOptionChanged;
            UdfEmbedIdCheck.Checked += OnOptionChanged;
            UdfEmbedIdCheck.Unchecked += OnOptionChanged;
            UdfSparseCheck.Checked += OnOptionChanged;
            UdfSparseCheck.Unchecked += OnOptionChanged;
            UdfLongAllocCheck.Checked += OnOptionChanged;
            UdfLongAllocCheck.Unchecked += OnOptionChanged;
            UdfReadmeTextBox.TextChanged += OnOptionChanged;
            DvdVideoZoneCheck.Checked += OnOptionChanged;
            DvdVideoZoneCheck.Unchecked += OnOptionChanged;
            DvdTruncateCheck.Checked += OnOptionChanged;
            DvdTruncateCheck.Unchecked += OnOptionChanged;
            IgnoreMaxSizeCheck.Checked += OnOptionChanged;
            IgnoreMaxSizeCheck.Unchecked += OnOptionChanged;
            RefreshCommand();
            SourceTextBox.Focus();
        }

        private void OnOptionChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
                return;
            RefreshCommand();
        }

        private void SourceBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog { Title = "选择源文件夹" };
            TrySetInitialDirectory(dialog, SourceTextBox.Text);
            if (dialog.ShowDialog() == true)
                SourceTextBox.Text = dialog.FolderName;
        }

        private void DestinationBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Title = "选择输出 ISO",
                Filter = "ISO 映像 (*.iso)|*.iso",
                DefaultExt = "iso",
                AddExtension = true,
                OverwritePrompt = true,
            };
            TrySetInitialDirectory(dialog, DestinationTextBox.Text);
            TrySetFileName(dialog, DestinationTextBox.Text);
            if (dialog.ShowDialog() == true)
                DestinationTextBox.Text = dialog.FileName;
        }

        private void JolietReadmeBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            BrowseReadme(JolietReadmeTextBox);
        }

        private void UdfReadmeBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            BrowseReadme(UdfReadmeTextBox);
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            OscdimgRequest request;
            try
            {
                request = ReadRequest();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var error = OscdimgCommand.Validate(request);
            if (error != null)
            {
                MessageBox.Show(this, error, Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!File.Exists(OscdimgRunner.ExePath))
            {
                MessageBox.Show(
                    this,
                    "找不到 bundled\\oscdimg.exe。请把 oscdimg.exe 放到程序目录的 bundled 文件夹中。",
                    Title,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var arguments = OscdimgCommand.Build(request);
            SetRunning(true);
            LogTextBox.Clear();
            AppendLog(OscdimgCommand.FormatCommandLine(OscdimgRunner.ExePath, arguments));

            _runCts = new CancellationTokenSource();
            var runner = new OscdimgRunner();
            try
            {
                var code = await runner.RunAsync(arguments, AppendLog, _runCts.Token);
                if (_runCts.IsCancellationRequested)
                    AppendLog("已停止。");
                else if (code == 0)
                    AppendLog("完成。");
                else
                {
                    AppendLog($"失败，退出码 {code}。");
                    MessageBox.Show(this, $"oscdimg 失败，退出码 {code}。", Title, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                AppendLog(ex.Message);
                MessageBox.Show(this, ex.Message, Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                runner.Dispose();
                _runCts.Dispose();
                _runCts = null;
                SetRunning(false);
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            StopButton.IsEnabled = false;
            _runCts?.Cancel();
        }

        private void RefreshCommand()
        {
            UpdatePanels();
            try
            {
                var arguments = OscdimgCommand.Build(ReadRequest());
                CommandTextBox.Text = OscdimgCommand.FormatCommandLine(OscdimgRunner.ExePath, arguments);
            }
            catch (Exception ex)
            {
                CommandTextBox.Text = ex.Message;
            }
        }

        private void UpdatePanels()
        {
            var format = SelectedFormat();
            IsoPanel.Visibility = format == ImageFormatKind.Iso9660 ? Visibility.Visible : Visibility.Collapsed;
            JolietPanel.Visibility = format == ImageFormatKind.Joliet ? Visibility.Visible : Visibility.Collapsed;
            UdfPanel.Visibility = format == ImageFormatKind.Udf ? Visibility.Visible : Visibility.Collapsed;
            DvdPanel.Visibility = format == ImageFormatKind.DvdVideo ? Visibility.Visible : Visibility.Collapsed;

            IsoLowerCheck.IsEnabled = IsoNtRadio.IsChecked != true;

            var jolietOnly = JolietOnlyRadio.IsChecked == true;
            JolietReadmeTextBox.IsEnabled = jolietOnly;
            JolietReadmeBrowseButton.IsEnabled = jolietOnly;

            var udfOnly = UdfOnlyRadio.IsChecked == true;
            UdfEmbedDataCheck.IsEnabled = udfOnly;
            UdfEmbedIdCheck.IsEnabled = udfOnly;
            UdfSparseCheck.IsEnabled = udfOnly;
            UdfReadmeTextBox.IsEnabled = udfOnly;
            UdfReadmeBrowseButton.IsEnabled = udfOnly;
        }

        private OscdimgRequest ReadRequest()
        {
            return new OscdimgRequest
            {
                Format = SelectedFormat(),
                SourceDirectory = NormalizeDirectory(SourceTextBox.Text),
                DestinationFile = NormalizeDestination(DestinationTextBox.Text),
                Label = LabelTextBox.Text.Trim(),
                IgnoreMaxSize = IgnoreMaxSizeCheck.IsChecked == true,
                IsoNameMode = SelectedIsoName(),
                PreserveLowerCase = IsoLowerCheck.IsChecked == true,
                JolietMode = JolietOnlyRadio.IsChecked == true ? JolietMode.JolietOnly : JolietMode.WithIso9660,
                JolietReadme = NormalizeOptionalFile(JolietReadmeTextBox.Text),
                UdfNamespace = UdfOnlyRadio.IsChecked == true ? UdfNamespace.UdfOnly : UdfNamespace.WithIso9660,
                UdfVersion = SelectedUdfVersion(),
                UdfEmbedData = UdfEmbedDataCheck.IsChecked == true,
                UdfEmbedIdentifier = UdfEmbedIdCheck.IsChecked == true,
                UdfSparse = UdfSparseCheck.IsChecked == true,
                UdfLongAllocation = UdfLongAllocCheck.IsChecked == true,
                UdfReadme = NormalizeOptionalFile(UdfReadmeTextBox.Text),
                DvdVideoZone = DvdVideoZoneCheck.IsChecked == true,
                DvdTruncateIso = DvdTruncateCheck.IsChecked == true,
            };
        }

        private ImageFormatKind SelectedFormat()
        {
            return FormatCombo.SelectedIndex switch
            {
                1 => ImageFormatKind.Joliet,
                2 => ImageFormatKind.Udf,
                3 => ImageFormatKind.DvdVideo,
                _ => ImageFormatKind.Iso9660,
            };
        }

        private IsoNameMode SelectedIsoName()
        {
            if (IsoNtRadio.IsChecked == true)
                return IsoNameMode.Nt351;
            if (IsoLongRadio.IsChecked == true)
                return IsoNameMode.LongNames;
            return IsoNameMode.Dos8Dot3;
        }

        private string SelectedUdfVersion()
        {
            return (UdfVersionCombo.SelectedItem as ComboBoxItem)?.Content as string ?? "1.02";
        }

        private void SetRunning(bool running)
        {
            FormPanel.IsEnabled = !running;
            CommandTextBox.IsEnabled = !running;
            StartButton.IsEnabled = !running;
            StopButton.IsEnabled = running;
            if (!running)
                RunProgress.Value = 0;
        }

        private void AppendLog(string? line)
        {
            if (line == null)
                return;
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => AppendLog(line));
                return;
            }

            LogTextBox.AppendText(line + Environment.NewLine);
            LogTextBox.ScrollToEnd();
            if (TryParseProgress(line, out var percent))
                RunProgress.Value = percent;
        }

        private static bool TryParseProgress(string line, out int percent)
        {
            percent = 0;
            var text = line.Trim();
            const string suffix = "% complete";
            if (!text.EndsWith(suffix, StringComparison.Ordinal))
                return false;
            var number = text[..^suffix.Length];
            return number.Length is > 0 and <= 3
                && int.TryParse(number, out percent)
                && percent is >= 0 and <= 100;
        }

        private static void BrowseReadme(TextBox target)
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择说明文件",
                Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
            };
            TrySetInitialDirectory(dialog, target.Text);
            TrySetFileName(dialog, target.Text);
            if (dialog.ShowDialog() == true)
                target.Text = dialog.FileName;
        }

        private static string NormalizeDirectory(string text)
        {
            var trimmed = text.Trim();
            if (trimmed.Length == 0)
                return "";
            return TryFullPath(trimmed, out var full) ? full : trimmed;
        }

        private static string NormalizeDestination(string text)
        {
            var trimmed = text.Trim();
            if (trimmed.Length == 0)
                return "";
            if (!Path.GetExtension(trimmed).Equals(".iso", StringComparison.OrdinalIgnoreCase))
                trimmed = Path.ChangeExtension(trimmed, ".iso");
            return TryFullPath(trimmed, out var full) ? full : trimmed;
        }

        private static string? NormalizeOptionalFile(string text)
        {
            var trimmed = text.Trim();
            if (trimmed.Length == 0)
                return null;
            return TryFullPath(trimmed, out var full) ? full : trimmed;
        }

        private static bool TryFullPath(string path, out string fullPath)
        {
            try
            {
                fullPath = Path.GetFullPath(path);
                return true;
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                fullPath = "";
                return false;
            }
        }

        private static void TrySetInitialDirectory(CommonItemDialog dialog, string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;
            try
            {
                var full = Path.GetFullPath(path.Trim());
                if (Directory.Exists(full))
                {
                    dialog.InitialDirectory = full;
                    return;
                }

                var directory = Path.GetDirectoryName(full);
                if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                    dialog.InitialDirectory = directory;
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
            }
        }

        private static void TrySetFileName(FileDialog dialog, string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;
            try
            {
                var name = Path.GetFileName(path.Trim());
                if (!string.IsNullOrEmpty(name))
                    dialog.FileName = name;
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
            {
            }
        }
    }
}
