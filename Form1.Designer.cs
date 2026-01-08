using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace lab9
{
    public partial class Form1 : Form
    {
        // --- UI ---
        private Panel panelSidebar = null!;
        private Panel panelTop = null!;
        private Panel panelContent = null!;

        private Label lblHub = null!;
        private Label lblTitle = null!;

        private Button btnLogin = null!;
        private Button btnCreate = null!;
        private Button btnSaloane = null!;

        // Content screens
        private Panel screenLogin = null!;
        private Panel screenCreate = null!;
        private Panel screenSaloane = null!;

        // Saloane screen controls
        private ListBox lbSaloane = null!;
        private TextBox tbNume = null!;
        private TextBox tbTelefon = null!;
        private Button btnCopy = null!;

        private readonly List<Salon> _saloane = new();

        public Form1()
        {
            // IMPORTANT: nu folosim Designer deloc
            // InitializeComponent();

            // Setup base form
            SetupForm();

            // Build UI
            BuildUi();
            SeedData();
            BindSaloane();

            // Default screen
            lblTitle.Text = "Saloane";
            ShowScreen(screenSaloane);
            SetActiveSidebar(btnSaloane);
        }

        private void SetupForm()
        {
            Text = "HUB";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1100, 650);
            BackColor = Color.FromArgb(185, 174, 207);
        }

        private void BuildUi()
        {
            // Root panels
            panelSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 220,
                BackColor = Color.FromArgb(46, 26, 92)
            };

            panelTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(122, 63, 242)
            };

            panelContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(185, 174, 207)
            };

            Controls.Clear();
            Controls.Add(panelContent);
            Controls.Add(panelTop);
            Controls.Add(panelSidebar);

            // Top title
            lblTitle = new Label
            {
                Text = "",
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                Location = new Point(20, 15)
            };
            panelTop.Controls.Add(lblTitle);

            // Sidebar HUB
            lblHub = new Label
            {
                Text = "HUB",
                AutoSize = false,
                Height = 70,
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                BackColor = Color.FromArgb(61, 33, 124)
            };
            panelSidebar.Controls.Add(lblHub);

            // Sidebar buttons
            btnSaloane = MakeSidebarButton("Saloane");
            btnCreate = MakeSidebarButton("Create Account");
            btnLogin = MakeSidebarButton("Login");

            btnSaloane.Click += (_, __) => { lblTitle.Text = "Saloane"; ShowScreen(screenSaloane); SetActiveSidebar(btnSaloane); };
            btnCreate.Click += (_, __) => { lblTitle.Text = "Create Account"; ShowScreen(screenCreate); SetActiveSidebar(btnCreate); };
            btnLogin.Click += (_, __) => { lblTitle.Text = "Login"; ShowScreen(screenLogin); SetActiveSidebar(btnLogin); };

            panelSidebar.Controls.Add(btnSaloane);
            panelSidebar.Controls.Add(btnCreate);
            panelSidebar.Controls.Add(btnLogin);

            // Build screens
            screenLogin = BuildLoginScreen();
            screenCreate = BuildCreateScreen();
            screenSaloane = BuildSaloaneScreen();

            panelContent.Controls.Add(screenSaloane);
            panelContent.Controls.Add(screenCreate);
            panelContent.Controls.Add(screenLogin);
        }

        private Button MakeSidebarButton(string text)
        {
            var b = new Button
            {
                Text = text,
                Dock = DockStyle.Top,
                Height = 90,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(58, 30, 120),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16, FontStyle.Bold)
            };

            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = Color.FromArgb(210, 210, 230);

            return b;
        }

        private void SetActiveSidebar(Button active)
        {
            foreach (Control c in panelSidebar.Controls)
            {
                if (c is Button b)
                    b.BackColor = Color.FromArgb(58, 30, 120);
            }
            active.BackColor = Color.FromArgb(122, 63, 242);
        }

        private void ShowScreen(Panel screen)
        {
            screenLogin.Visible = false;
            screenCreate.Visible = false;
            screenSaloane.Visible = false;

            screen.Dock = DockStyle.Fill;
            screen.Visible = true;
            screen.BringToFront();
        }

        private Panel BuildLoginScreen()
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(185, 174, 207) };

            var lbl = new Label
            {
                Text = "Login (placeholder)",
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                Location = new Point(60, 60)
            };
            p.Controls.Add(lbl);

            return p;
        }

        private Panel BuildCreateScreen()
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(185, 174, 207) };

            var lbl = new Label
            {
                Text = "Create Account (placeholder)",
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                Location = new Point(60, 60)
            };
            p.Controls.Add(lbl);

            return p;
        }

        private Panel BuildSaloaneScreen()
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(185, 174, 207) };

            // Group List (stanga)
            var gbList = new GroupBox
            {
                Text = "Lista saloane",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(50, 60),
                Size = new Size(280, 460)
            };

            lbSaloane = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };
            lbSaloane.SelectedIndexChanged += (_, __) => ShowSelectedSalon();
            gbList.Controls.Add(lbSaloane);

            // Group Details (dreapta)
            var gbDetails = new GroupBox
            {
                Text = "Detalii salon",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(360, 60),
                Size = new Size(620, 460)
            };

            var lblNume = new Label
            {
                Text = "Nume",
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(40, 70)
            };

            tbNume = new TextBox
            {
                Location = new Point(160, 68),
                Width = 380,
                ReadOnly = true,
                Font = new Font("Segoe UI", 11, FontStyle.Regular)
            };

            var lblTelefon = new Label
            {
                Text = "Telefon",
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(40, 140)
            };

            tbTelefon = new TextBox
            {
                Location = new Point(160, 138),
                Width = 260,
                ReadOnly = true,
                Font = new Font("Segoe UI", 11, FontStyle.Regular)
            };

            btnCopy = new Button
            {
                Text = "Copiază",
                Location = new Point(430, 136),
                Size = new Size(110, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(122, 63, 242),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnCopy.FlatAppearance.BorderSize = 1;
            btnCopy.Click += (_, __) => CopyPhone();

            gbDetails.Controls.Add(lblNume);
            gbDetails.Controls.Add(tbNume);
            gbDetails.Controls.Add(lblTelefon);
            gbDetails.Controls.Add(tbTelefon);
            gbDetails.Controls.Add(btnCopy);

            p.Controls.Add(gbList);
            p.Controls.Add(gbDetails);

            return p;
        }

        private void SeedData()
        {
            _saloane.Clear();
            _saloane.AddRange(new[]
            {
                new Salon("Salon Velvet", "+40 721 111 111"),
                new Salon("Glow Studio", "+40 721 222 222"),
                new Salon("HairCraft", "+40 721 333 333"),
                new Salon("Queen Cuts", "+40 721 444 444"),
                new Salon("Urban Style", "+40 721 555 555"),
                new Salon("Elegance Beauty", "+40 721 666 666"),
                new Salon("Bliss Hair", "+40 721 777 777"),
                new Salon("Luna Salon", "+40 721 888 888"),
                new Salon("Diamond Look", "+40 721 999 999"),
                new Salon("Studio Nova", "+40 722 000 000"),
            });
        }

        private void BindSaloane()
        {
            lbSaloane.DisplayMember = nameof(Salon.Name);
            lbSaloane.DataSource = null;
            lbSaloane.DataSource = _saloane;

            if (_saloane.Count > 0)
                lbSaloane.SelectedIndex = 0;
        }

        private void ShowSelectedSalon()
        {
            if (lbSaloane.SelectedItem is not Salon s)
            {
                tbNume.Text = "";
                tbTelefon.Text = "";
                return;
            }

            tbNume.Text = s.Name;
            tbTelefon.Text = s.Phone;
        }

        private void CopyPhone()
        {
            var phone = tbTelefon.Text?.Trim();
            if (string.IsNullOrEmpty(phone))
            {
                MessageBox.Show("Nu există număr de copiat.");
                return;
            }

            Clipboard.SetText(phone);
            MessageBox.Show("Numărul a fost copiat.");
        }
    }

    public sealed class Salon
    {
        public string Name { get; }
        public string Phone { get; }

        public Salon(string name, string phone)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Phone = phone ?? throw new ArgumentNullException(nameof(phone));
        }

        public override string ToString() => Name;
    }
}
