using System;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

// ReSharper disable UnusedMember.Global
// ReSharper disable All

namespace GraphDisplay
{
    public partial class Form1 : Form
    {
        private Graph _g;
        private readonly Timer _t;

        public Form1()
        {
            InitializeComponent();
            typeof(Form).InvokeMember("DoubleBuffered",
                BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic, null, this,
                new object[] {true});
            _t = new Timer
            {
                Enabled = false,
                Interval = 10
            };
            _t.Tick += Reload;
        }

        private void Reload(object sender, EventArgs e)
        {
            _g.Update();
            Invalidate();
        }
        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            _g.Draw(e.Graphics, Color.Black);
        }
        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            _g.MouseRelease();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            _g = new Graph(ClientRectangle);
            WebPage.SetWebsite("en.wikipedia.org", "/wiki/");
            _g.CreateThread = new Thread(BuildGraph);
            _g.CreateThread.Start("");
            _g.MouseMove(0, 0);
            _t.Enabled = true;
        }
        private void BuildGraph(object url)
        {
            WebPage root = new WebPage(url.ToString());
            WebPage.BreadthWiseCreate(5, 100, root, _g);
            _g.ResetNodes();
        }
        private void Form1_Scroll(object sender, MouseEventArgs e)
        {
            _g.HandleScroll(e.Delta <= 0);
        }
        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            _g.MouseMove(e.X, e.Y);
        }
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.ControlKey:
                {
                    if (_g.Remove != 1)
                        _g.Remove++;
                    break;
                }
                case Keys.Space:
                    _g.Paused = !_g.Paused;
                    break;
            }
        }
        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ControlKey)
                _g.Remove--;
        }
        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            _g.MousePress(e.Button == MouseButtons.Right);
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            _g.CreateThread.Abort();
        }
    }
}