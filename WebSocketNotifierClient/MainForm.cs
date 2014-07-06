using System;
using System.Threading;
using System.Windows.Forms;
using WebSocketNotifierClient.Properties;
using WebSocketSharp;

namespace WebSocketNotifierClient
{
    public partial class MainForm : Form
    {
        private WebSocket _client;

        public MainForm()
        {
            InitializeComponent();

            _notifyIcon.Text = Text;
            _notifyIcon.Icon = Resources.announcements;
            Application.ThreadException += OnThreadException;
            Application.ApplicationExit += OnApplicationExit;
        }

        private void OnApplicationExit(object sender, EventArgs e)
        {
            if (_client != null)
            {
                _client.Close();
                _client = null;
            }
        }

        private void OnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            ShowBalloon(ToolTipIcon.Error, e.Exception.Message);
        }

        private void OnError(object sender, ErrorEventArgs message)
        {
            ShowBalloon(ToolTipIcon.Error, message.Message);
        }

        private void OnRecievedMessage(object sender, MessageEventArgs message)
        {
            var result = BuildResult.Parse(message.Data);
            var msg = result.ToMessage();

            if (result.IsFailure)
            {
                ShowBalloon(ToolTipIcon.Info, msg);
                PlaySound(result);
                return;
            }

            if (Settings.Default.ShowErrorOnly)
            {
                return;
            }


            if (result.IsSuccess)
            {
                ShowBalloon(ToolTipIcon.Info, msg);
                PlaySound(result);
            }
            else if (result.IsUnstable)
            {
                ShowBalloon(ToolTipIcon.Warning, msg);
                PlaySound(result);
            }
            else if (result.IsAborted)
            {
                ShowBalloon(ToolTipIcon.Info, msg);
                PlaySound(result);
            }
        }

        private void PlaySound(BuildResult result)
        {
            var path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            var file = "";

            if (result.IsSuccess)
            {
                file = "success.wav";
            }
            else if (result.IsFailure)
            {
                file = "failure.wav";
            }
            else if (result.IsUnstable)
            {
                file = "unstable.wav";
            }
            else if (result.IsAborted)
            {
                file = "aborted.wav";
            }
            else
            {
                return;
            }

            var wavPath = System.IO.Path.Combine(path, file);
            if (System.IO.File.Exists(wavPath))
            {
                try
                {
                    using (var player = new System.Media.SoundPlayer(wavPath))
                    {
                        player.PlaySync();
                    }
                }
                catch (Exception)
                {
                    /* Do nothing */
                    throw;
                }
            }
        }


        private void ShowBalloon(ToolTipIcon icon, string message)
        {
            _notifyIcon.BalloonTipIcon = icon;
            _notifyIcon.BalloonTipTitle = Resources.WebSocketServerName;
            _notifyIcon.BalloonTipText = message;
            _notifyIcon.ShowBalloonTip((int)(Settings.Default.BalloonTipTimeout * 1000));
        }

        public void OpenClient()
        {
            if (_client != null)
            {
                _client.OnMessage -= OnRecievedMessage;
                _client.OnError -= OnError;
                _client.Close();
            }
            _client = new WebSocket(Settings.Default.Url);
            _client.OnMessage += OnRecievedMessage;
            _client.OnError += OnError;
            _client.Connect();
        }

        private void _exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void _settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dialog = new SettingDialog();
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                OpenClient();
            }
        }
    }
}
