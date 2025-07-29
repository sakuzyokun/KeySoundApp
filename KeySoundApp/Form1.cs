using NAudio.Wave;
using System;
using System.IO;
using System.Windows.Forms;
using System.Drawing;

namespace KeySoundApp
{
    public partial class Form1 : Form
    {
        private KeyboardHook _hook = new KeyboardHook();
        private NotifyIcon notifyIcon;
        private ContextMenuStrip trayMenu;
        public Form1()
        {
            InitializeComponent();

            // 前回の設定をUIに反映
            checkBoxEnableSound.Checked = Properties.Settings.Default.EnableSound;
            labelFilePath.Text = Path.GetFileName(Properties.Settings.Default.SoundFilePath);
            trackBarVolume.Value = Properties.Settings.Default.VolumePercent;
            labelVolume.Text = trackBarVolume.Value + "%";

            Console.WriteLine("有効か: " + Properties.Settings.Default.EnableSound);
            Console.WriteLine("音ファイル: " + Properties.Settings.Default.SoundFilePath);
            Console.WriteLine("音量(％): " + Properties.Settings.Default.VolumePercent);
            Console.WriteLine("ファイル存在する？: " + File.Exists(Properties.Settings.Default.SoundFilePath));

            // トレイアイコンのセットアップ
            SetupTrayIcon();
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.Hide();

            // 設定読み込み（UI更新）
            checkBoxEnableSound.Checked = Properties.Settings.Default.EnableSound;
            labelFilePath.Text = Path.GetFileName(Properties.Settings.Default.SoundFilePath);
            trackBarVolume.Value = Math.Max(0, Math.Min(100, Properties.Settings.Default.VolumePercent));
            labelVolume.Text = trackBarVolume.Value + "%";

            // Hookスタート
            _hook.KeyPressed += (s, args) => PlayKeySound();
            _hook.Start();
        }
        private void SetupTrayIcon()
        {
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("設定", null, (s, e) => ShowSettings());
            trayMenu.Items.Add("終了", null, (s, e) => Application.Exit());

            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = SystemIcons.Error; // ← アイコン変えたいなら .ico ファイル指定可
            notifyIcon.Text = "KeySoundApp";
            notifyIcon.Visible = true;
            notifyIcon.ContextMenuStrip = trayMenu;
            notifyIcon.DoubleClick += (s, e) => ShowSettings(); // ダブルクリックで設定開く
        }
        private void ShowSettings()
        {
            this.Invoke((MethodInvoker)(() =>
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
                this.BringToFront();
            }));
        }

        private void checkBoxEnableSound_CheckedChanged(object sender, EventArgs e)
        {
            // 必要ならリアルタイムに設定変更もできる
        }

        private void buttonSelectFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "WAVファイル|*.wav";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                Properties.Settings.Default.SoundFilePath = ofd.FileName;
                labelFilePath.Text = Path.GetFileName(ofd.FileName);
            }
        }

        private void trackBarVolume_Scroll(object sender, EventArgs e)
        {
            Properties.Settings.Default.VolumePercent = trackBarVolume.Value;
            labelVolume.Text = trackBarVolume.Value + "%";
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.EnableSound = checkBoxEnableSound.Checked;
            Properties.Settings.Default.VolumePercent = trackBarVolume.Value;
            Properties.Settings.Default.Save();

            MessageBox.Show("設定を保存しました！");
        }
        public void PlayKeySound()
        {
            if (!Properties.Settings.Default.EnableSound)
                return;

            string filePath = Properties.Settings.Default.SoundFilePath;
            if (!File.Exists(filePath)) return;

            float volume = Properties.Settings.Default.VolumePercent / 100f;

            // 音を非同期に再生する（再生完了を待たない）
            _ = System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    using (var audioFile = new AudioFileReader(filePath))
                    using (var outputDevice = new WaveOutEvent())
                    {
                        audioFile.Volume = volume;
                        outputDevice.Init(audioFile);
                        outputDevice.Play();

                        // 音の再生が終わるまで待つ（終わったら自動で解放される）
                        while (outputDevice.PlaybackState == PlaybackState.Playing)
                        {
                            System.Threading.Thread.Sleep(10);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("音再生エラー: " + ex.Message);
                }
            });
        }
        private void buttonClose_Click(object sender, EventArgs e)
        {
            this.Hide(); // 閉じるんじゃなく非表示
        }
    }
}
