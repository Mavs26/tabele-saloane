using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace lab9
{
    public class Form1 : Form
    {
        private readonly string _currentUser;

        // ===== DB =====
        private const string DbName = "hub_saloane";
        private static readonly string CsMaster =
            @"Server=(localdb)\MSSQLLocalDB;Integrated Security=true;Initial Catalog=master;TrustServerCertificate=True;";

        private static readonly string CsDb =
            $@"Server=(localdb)\MSSQLLocalDB;Integrated Security=true;Initial Catalog={DbName};TrustServerCertificate=True;";

        // ===== Root UI =====
        private Panel panelSidebar = null!;
        private Panel panelTop = null!;
        private Panel panelContent = null!;

        private Label lblHub = null!;
        private Label lblTitle = null!;
        private Label lblUser = null!;

        private Button btnSaloane = null!;
        private Button btnProgramari = null!;
        private Button btnLogout = null!;

        // ===== Screens =====
        private Panel screenSaloane = null!;
        private Panel screenProgramari = null!;

        // ===== Saloane controls =====
        private ListBox lbSaloane = null!;
        private TextBox tbNume = null!;
        private TextBox tbTelefon = null!;
        private Button btnCopy = null!;

        // ===== Programari controls =====
        private DataGridView dgvProg = null!;
        private ComboBox cbSalonProg = null!;
        private DateTimePicker dtpDate = null!;
        private DateTimePicker dtpTime = null!;
        private TextBox tbClientName = null!;
        private TextBox tbClientPhone = null!;
        private TextBox tbNotes = null!;
        private Button btnAddProg = null!;
        private Button btnDeleteProg = null!;
        private Button btnRefreshProg = null!;

        // ===== Data cache =====
        private readonly List<Salon> _saloane = new();

        public Form1(string currentUser)
        {
            _currentUser = string.IsNullOrWhiteSpace(currentUser) ? "unknown" : currentUser;

            SetupForm();

            Shown += (_, __) =>
            {
                EnsureDatabaseAndSchema();
                SeedSaloaneIfEmpty();

                BuildUi();

                ReloadSaloaneFromDb();
                BindSaloane();
                ReloadProgramariFromDb();

                lblTitle.Text = "Saloane";
                ShowScreen(screenSaloane);
                SetActiveSidebar(btnSaloane);
            };
        }

        private void SetupForm()
        {
            Text = "Panel"; // <— schimbat din HUB
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1200, 720);
            BackColor = Theme.Page;
        }

        // =========================================================
        // DB INIT
        // =========================================================
        private static void EnsureDatabaseAndSchema()
        {
            using (var conn = new SqlConnection(CsMaster))
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();
                cmd.CommandText = $@"
IF DB_ID(N'{DbName}') IS NULL
BEGIN
    CREATE DATABASE [{DbName}];
END";
                cmd.ExecuteNonQuery();
            }

            using (var conn = new SqlConnection(CsDb))
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();

                cmd.CommandText = @"
IF OBJECT_ID(N'dbo.saloane', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.saloane (
        salon_id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_saloane PRIMARY KEY,
        salon_name NVARCHAR(120) NOT NULL,
        salon_phone NVARCHAR(40) NOT NULL
    );
END;

IF OBJECT_ID(N'dbo.programari', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.programari (
        prog_id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_programari PRIMARY KEY,
        salon_id INT NOT NULL,
        client_name NVARCHAR(120) NOT NULL,
        client_phone NVARCHAR(40) NOT NULL,
        start_at DATETIME2(0) NOT NULL,
        notes NVARCHAR(300) NULL,
        created_at DATETIME2(0) NOT NULL CONSTRAINT DF_programari_created DEFAULT SYSDATETIME(),
        CONSTRAINT FK_programari_saloane FOREIGN KEY (salon_id) REFERENCES dbo.saloane(salon_id)
    );

    CREATE INDEX IX_programari_start_at ON dbo.programari(start_at);
    CREATE INDEX IX_programari_salon ON dbo.programari(salon_id);
END;";
                cmd.ExecuteNonQuery();
            }
        }

        private static void SeedSaloaneIfEmpty()
        {
            using (var conn = new SqlConnection(CsDb))
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();
                cmd.CommandText = "SELECT COUNT(1) FROM dbo.saloane;";
                var count = (int)cmd.ExecuteScalar()!;
                if (count > 0) return;

                var seeds = new (string name, string phone)[]
                {
                    ("Salon Velvet", "+40 721 111 111"),
                    ("Glow Studio", "+40 721 222 222"),
                    ("HairCraft", "+40 721 333 333"),
                    ("Queen Cuts", "+40 721 444 444"),
                    ("Urban Style", "+40 721 555 555"),
                    ("Elegance Beauty", "+40 721 666 666"),
                    ("Bliss Hair", "+40 721 777 777"),
                    ("Luna Salon", "+40 721 888 888"),
                    ("Diamond Look", "+40 721 999 999"),
                    ("Studio Nova", "+40 722 000 000"),
                };

                cmd.CommandText = "INSERT INTO dbo.saloane(salon_name, salon_phone) VALUES (@n, @p);";
                cmd.Parameters.Add("@n", SqlDbType.NVarChar, 120);
                cmd.Parameters.Add("@p", SqlDbType.NVarChar, 40);

                foreach (var s in seeds)
                {
                    cmd.Parameters["@n"].Value = s.name;
                    cmd.Parameters["@p"].Value = s.phone;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // =========================================================
        // UI BUILD
        // =========================================================
        private void BuildUi()
        {
            panelSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 240,
                BackColor = Theme.Sidebar
            };

            panelTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Theme.Primary
            };

            panelContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Panel
            };

            Controls.Clear();
            Controls.Add(panelContent);
            Controls.Add(panelTop);
            Controls.Add(panelSidebar);

            lblTitle = new Label
            {
                AutoSize = true,
                ForeColor = Theme.White,
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                Location = new Point(20, 15)
            };
            panelTop.Controls.Add(lblTitle);

            lblUser = new Label
            {
                AutoSize = true,
                ForeColor = Theme.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Text = $"User: {_currentUser}",
                Top = 23
            };
            panelTop.Controls.Add(lblUser);

            void PositionUserLabel()
            {
                lblUser.Left = panelTop.Width - lblUser.Width - 20;
            }

            panelTop.Resize += (_, __) => PositionUserLabel();
            PositionUserLabel();

            lblHub = new Label
            {
                Text = "Panel", // <— schimbat din HUB
                Dock = DockStyle.Top,
                Height = 110,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Theme.White,
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                BackColor = Theme.PrimaryDark
            };

            btnSaloane = MakeSidebarButton("Saloane");
            btnProgramari = MakeSidebarButton("Programari");
            btnLogout = MakeSidebarButton("Logout");

            btnSaloane.Click += (_, __) =>
            {
                lblTitle.Text = "Saloane";
                ReloadSaloaneFromDb();
                BindSaloane();
                ShowScreen(screenSaloane);
                SetActiveSidebar(btnSaloane);
            };

            btnProgramari.Click += (_, __) =>
            {
                lblTitle.Text = "Programari";
                ReloadSaloaneFromDb();
                BindSaloaneCombo(cbSalonProg);
                ReloadProgramariFromDb();
                ShowScreen(screenProgramari);
                SetActiveSidebar(btnProgramari);
            };

            btnLogout.Click += (_, __) =>
            {
                var ok = MessageBox.Show("Vrei să ieși din aplicație?", "Logout",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (ok == DialogResult.Yes)
                    Close();
            };

            panelSidebar.Controls.Clear();
            panelSidebar.Controls.Add(btnLogout);
            panelSidebar.Controls.Add(btnProgramari);
            panelSidebar.Controls.Add(btnSaloane);
            panelSidebar.Controls.Add(lblHub);

            screenSaloane = BuildSaloaneScreen();
            screenProgramari = BuildProgramariScreen();

            panelContent.Controls.Clear();
            panelContent.Controls.Add(screenProgramari);
            panelContent.Controls.Add(screenSaloane);
        }

        private Button MakeSidebarButton(string text)
        {
            var b = new Button
            {
                Text = text,
                Dock = DockStyle.Top,
                Height = 90,
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.PrimaryDark,
                ForeColor = Theme.White,
                Font = new Font("Segoe UI", 14, FontStyle.Bold)
            };
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = Theme.Border;
            return b;
        }

        private void SetActiveSidebar(Button active)
        {
            foreach (Control c in panelSidebar.Controls)
                if (c is Button b) b.BackColor = Theme.PrimaryDark;

            active.BackColor = Theme.Primary;
        }

        private void ShowScreen(Panel screen)
        {
            screenSaloane.Visible = false;
            screenProgramari.Visible = false;

            screen.Dock = DockStyle.Fill;
            screen.Visible = true;
            screen.BringToFront();
        }

        private Panel BaseScreen() =>
            new Panel { Dock = DockStyle.Fill, BackColor = Theme.Panel };

        private Label MakeLabel(string text, int x, int y) =>
            new Label
            {
                Text = text,
                AutoSize = true,
                ForeColor = Theme.PrimaryDark,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(x, y)
            };

        private TextBox MakeTextBox(int x, int y, int width = 320, bool readOnly = false)
        {
            return new TextBox
            {
                Location = new Point(x, y),
                Width = width,
                Font = new Font("Segoe UI", 11),
                ReadOnly = readOnly
            };
        }

        private Button MakeActionButton(string text, int x, int y, int w, int h, Action onClick)
        {
            var b = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, h),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Primary,
                ForeColor = Theme.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = Theme.Border;
            b.Click += (_, __) => onClick();
            return b;
        }

        // =========================================================
        // SCREENS
        // =========================================================
        private Panel BuildSaloaneScreen()
        {
            var p = BaseScreen();

            var gbList = new GroupBox
            {
                Text = "Lista saloane",
                ForeColor = Theme.PrimaryDark,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(80, 110),
                Size = new Size(360, 470)
            };

            lbSaloane = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            lbSaloane.SelectedIndexChanged += (_, __) => ShowSelectedSalon();
            gbList.Controls.Add(lbSaloane);

            var gbDetails = new GroupBox
            {
                Text = "Detalii salon",
                ForeColor = Theme.PrimaryDark,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(470, 110),
                Size = new Size(720, 470)
            };

            gbDetails.Controls.Add(MakeLabel("Nume", 80, 140));
            tbNume = MakeTextBox(210, 138, 430, readOnly: true);
            gbDetails.Controls.Add(tbNume);

            gbDetails.Controls.Add(MakeLabel("Telefon", 80, 220));
            tbTelefon = MakeTextBox(210, 218, 300, readOnly: true);
            gbDetails.Controls.Add(tbTelefon);

            btnCopy = MakeActionButton("Copiază", 520, 216, 120, 36, CopyPhone);
            gbDetails.Controls.Add(btnCopy);

            p.Controls.Add(gbList);
            p.Controls.Add(gbDetails);
            return p;
        }

        private Panel BuildProgramariScreen()
        {
            var p = BaseScreen();

            dgvProg = new DataGridView
            {
                Location = new Point(80, 95),
                Size = new Size(1110, 300),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White
            };

            var gbAdd = new GroupBox
            {
                Text = "Adaugă programare",
                ForeColor = Theme.PrimaryDark,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(80, 410),
                Size = new Size(760, 220)
            };

            gbAdd.Controls.Add(MakeLabel("Salon", 35, 45));
            cbSalonProg = new ComboBox
            {
                Location = new Point(140, 42),
                Width = 280,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 11, FontStyle.Regular)
            };
            gbAdd.Controls.Add(cbSalonProg);

            gbAdd.Controls.Add(MakeLabel("Data", 35, 90));
            dtpDate = new DateTimePicker
            {
                Location = new Point(140, 88),
                Width = 160,
                Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 11, FontStyle.Regular)
            };
            gbAdd.Controls.Add(dtpDate);

            gbAdd.Controls.Add(MakeLabel("Ora", 320, 90));
            dtpTime = new DateTimePicker
            {
                Location = new Point(370, 88),
                Width = 120,
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true,
                Font = new Font("Segoe UI", 11, FontStyle.Regular)
            };
            gbAdd.Controls.Add(dtpTime);

            gbAdd.Controls.Add(MakeLabel("Client", 35, 135));
            tbClientName = MakeTextBox(140, 132, 350);
            gbAdd.Controls.Add(tbClientName);

            gbAdd.Controls.Add(MakeLabel("Telefon", 510, 135));
            tbClientPhone = MakeTextBox(590, 132, 140);
            gbAdd.Controls.Add(tbClientPhone);

            gbAdd.Controls.Add(MakeLabel("Note", 35, 180));
            tbNotes = MakeTextBox(140, 177, 590);
            gbAdd.Controls.Add(tbNotes);

            var gbActions = new GroupBox
            {
                Text = "Acțiuni",
                ForeColor = Theme.PrimaryDark,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(860, 410),
                Size = new Size(330, 220)
            };

            btnAddProg = MakeActionButton("Adaugă", 40, 45, 250, 45, AddProgramare);
            btnDeleteProg = MakeActionButton("Șterge selectat", 40, 105, 250, 45, DeleteSelectedProgramare);
            btnRefreshProg = MakeActionButton("Refresh", 40, 165, 250, 40, ReloadProgramariFromDb);

            gbActions.Controls.Add(btnAddProg);
            gbActions.Controls.Add(btnDeleteProg);
            gbActions.Controls.Add(btnRefreshProg);

            p.Controls.Add(dgvProg);
            p.Controls.Add(gbAdd);
            p.Controls.Add(gbActions);

            return p;
        }

        // =========================================================
        // SALOANE (DB)
        // =========================================================
        private void ReloadSaloaneFromDb()
        {
            _saloane.Clear();

            using (var conn = new SqlConnection(CsDb))
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();
                cmd.CommandText = "SELECT salon_id, salon_name, salon_phone FROM dbo.saloane ORDER BY salon_name;";
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        _saloane.Add(new Salon(
                            r.GetInt32(0),
                            r.GetString(1),
                            r.GetString(2)
                        ));
                    }
                }
            }
        }

        private void BindSaloane()
        {
            lbSaloane.DisplayMember = nameof(Salon.Name);
            lbSaloane.ValueMember = nameof(Salon.Id);

            lbSaloane.DataSource = null;
            lbSaloane.DataSource = _saloane;

            if (_saloane.Count > 0)
                lbSaloane.SelectedIndex = 0;
        }

        private void BindSaloaneCombo(ComboBox combo)
        {
            combo.DisplayMember = nameof(Salon.Name);
            combo.ValueMember = nameof(Salon.Id);
            combo.DataSource = null;
            combo.DataSource = new List<Salon>(_saloane);
            if (combo.Items.Count > 0) combo.SelectedIndex = 0;
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

        // =========================================================
        // PROGRAMARI (DB)
        // =========================================================
        private void ReloadProgramariFromDb()
        {
            if (dgvProg == null) return;

            var dt = new DataTable();

            using (var conn = new SqlConnection(CsDb))
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();
                cmd.CommandText = @"
SELECT
    p.prog_id AS Id,
    s.salon_name AS Salon,
    p.start_at AS DataOra,
    p.client_name AS Client,
    p.client_phone AS Telefon,
    p.notes AS Note
FROM dbo.programari p
JOIN dbo.saloane s ON s.salon_id = p.salon_id
ORDER BY p.start_at DESC;";
                using (var ad = new SqlDataAdapter(cmd))
                    ad.Fill(dt);
            }

            dgvProg.DataSource = dt;
            if (dgvProg.Columns["Id"] != null) dgvProg.Columns["Id"].Visible = false;
            if (dgvProg.Columns["DataOra"] != null)
                dgvProg.Columns["DataOra"].DefaultCellStyle.Format = "dd.MM.yyyy HH:mm";
        }

        private void AddProgramare()
        {
            if (cbSalonProg.SelectedItem is not Salon salon)
            {
                MessageBox.Show("Selectează un salon.");
                return;
            }

            var client = tbClientName.Text.Trim();
            var phone = tbClientPhone.Text.Trim();
            var notes = tbNotes.Text.Trim();

            if (string.IsNullOrWhiteSpace(client))
            {
                MessageBox.Show("Completează numele clientului.");
                return;
            }

            if (string.IsNullOrWhiteSpace(phone))
            {
                MessageBox.Show("Completează telefonul clientului.");
                return;
            }

            var startAt = dtpDate.Value.Date.Add(dtpTime.Value.TimeOfDay);

            using (var conn = new SqlConnection(CsDb))
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();
                cmd.CommandText = @"
INSERT INTO dbo.programari(salon_id, client_name, client_phone, start_at, notes)
VALUES (@sid, @cn, @cp, @sa, @nt);";
                cmd.Parameters.AddWithValue("@sid", salon.Id);
                cmd.Parameters.AddWithValue("@cn", client);
                cmd.Parameters.AddWithValue("@cp", phone);
                cmd.Parameters.AddWithValue("@sa", startAt);
                cmd.Parameters.AddWithValue("@nt", string.IsNullOrWhiteSpace(notes) ? (object)DBNull.Value : notes);
                cmd.ExecuteNonQuery();
            }

            tbClientName.Text = "";
            tbClientPhone.Text = "";
            tbNotes.Text = "";

            ReloadProgramariFromDb();
            MessageBox.Show("Programare adăugată.");
        }

        private void DeleteSelectedProgramare()
        {
            if (dgvProg.CurrentRow == null)
            {
                MessageBox.Show("Selectează o programare din listă.");
                return;
            }

            var idObj = dgvProg.CurrentRow.Cells["Id"]?.Value;
            if (idObj == null || idObj == DBNull.Value)
            {
                MessageBox.Show("Nu pot identifica programarea selectată.");
                return;
            }

            var id = Convert.ToInt32(idObj);

            var ok = MessageBox.Show("Ștergi programarea selectată?", "Confirmare",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (ok != DialogResult.Yes) return;

            using (var conn = new SqlConnection(CsDb))
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();
                cmd.CommandText = "DELETE FROM dbo.programari WHERE prog_id = @id;";
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }

            ReloadProgramariFromDb();
        }
    }

    public sealed class Salon
    {
        public int Id { get; }
        public string Name { get; }
        public string Phone { get; }

        public Salon(int id, string name, string phone)
        {
            Id = id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Phone = phone ?? throw new ArgumentNullException(nameof(phone));
        }

        public override string ToString() => Name;
    }
}
