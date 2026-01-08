using System;
using System.Data;
using System.Drawing;
using System.Security.Cryptography;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace lab9
{
    public sealed class AuthForm : Form
    {
        private const string DbName = "hub_saloane";

        private static readonly string CsMaster =
            @"Server=(localdb)\MSSQLLocalDB;Integrated Security=true;Initial Catalog=master;TrustServerCertificate=True;";

        private static readonly string CsDb =
            $@"Server=(localdb)\MSSQLLocalDB;Integrated Security=true;Initial Catalog={DbName};TrustServerCertificate=True;";

        public string LoggedInUser { get; private set; } = "";

        private readonly Button _btnTabLogin;
        private readonly Button _btnTabCreate;

        private readonly Panel _panelLogin;
        private readonly Panel _panelCreate;

        private readonly TextBox _tbLoginUser;
        private readonly TextBox _tbLoginPass;

        private readonly TextBox _tbCreateUser;
        private readonly TextBox _tbCreatePass;
        private readonly TextBox _tbCreatePass2;

        public AuthForm()
        {
            EnsureDatabaseAndUsersTable();
            SeedAdminIfEmpty();

            Text = "Autentificare";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = true;
            ClientSize = new Size(520, 360);
            BackColor = Theme.Panel;

            var top = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Theme.Primary
            };
            Controls.Add(top);

            var title = new Label
            {
                Text = "Bun Venit",
                AutoSize = true,
                ForeColor = Theme.White,
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                Location = new Point(20, 15)
            };
            top.Controls.Add(title);

            _btnTabLogin = MakeTabButton("Login", 20, 85);
            _btnTabCreate = MakeTabButton("Create Account", 180, 85);

            _btnTabLogin.Click += (_, __) => ShowTab(isLogin: true);
            _btnTabCreate.Click += (_, __) => ShowTab(isLogin: false);

            Controls.Add(_btnTabLogin);
            Controls.Add(_btnTabCreate);

            _panelLogin = new Panel { Location = new Point(20, 130), Size = new Size(480, 210) };
            _panelCreate = new Panel { Location = new Point(20, 130), Size = new Size(480, 210) };

            Controls.Add(_panelLogin);
            Controls.Add(_panelCreate);

            // LOGIN
            _panelLogin.Controls.Add(MakeLabel("Username", 10, 15));
            _tbLoginUser = MakeTextBox(140, 12, 300, password: false);
            _panelLogin.Controls.Add(_tbLoginUser);

            _panelLogin.Controls.Add(MakeLabel("Parola", 10, 65));
            _tbLoginPass = MakeTextBox(140, 62, 300, password: true);
            _panelLogin.Controls.Add(_tbLoginPass);

            var btnLogin = MakeActionButton("Login", 140, 120, DoLogin);
            _panelLogin.Controls.Add(btnLogin);

            var btnExit1 = MakeSecondaryButton("Ieșire", 300, 120, CloseCancel);
            _panelLogin.Controls.Add(btnExit1);

            _tbLoginPass.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    DoLogin();
                }
            };

            // CREATE
            _panelCreate.Controls.Add(MakeLabel("Username", 10, 15));
            _tbCreateUser = MakeTextBox(140, 12, 300, password: false);
            _panelCreate.Controls.Add(_tbCreateUser);

            _panelCreate.Controls.Add(MakeLabel("Parola", 10, 65));
            _tbCreatePass = MakeTextBox(140, 62, 300, password: true);
            _panelCreate.Controls.Add(_tbCreatePass);

            _panelCreate.Controls.Add(MakeLabel("Confirmare", 10, 115));
            _tbCreatePass2 = MakeTextBox(140, 112, 300, password: true);
            _panelCreate.Controls.Add(_tbCreatePass2);

            var btnCreate = MakeActionButton("Creează", 140, 160, DoCreate);
            _panelCreate.Controls.Add(btnCreate);

            var btnExit2 = MakeSecondaryButton("Ieșire", 300, 160, CloseCancel);
            _panelCreate.Controls.Add(btnExit2);

            ShowTab(isLogin: true);
        }

        private void ShowTab(bool isLogin)
        {
            _panelLogin.Visible = isLogin;
            _panelCreate.Visible = !isLogin;

            _btnTabLogin.BackColor = isLogin ? Theme.Primary : Theme.PrimaryDark;
            _btnTabCreate.BackColor = !isLogin ? Theme.Primary : Theme.PrimaryDark;

            if (isLogin) _tbLoginUser.Focus();
            else _tbCreateUser.Focus();
        }

        private void DoLogin()
        {
            var u = _tbLoginUser.Text.Trim();
            var p = _tbLoginPass.Text;

            if (string.IsNullOrWhiteSpace(u) || string.IsNullOrEmpty(p))
            {
                MessageBox.Show("Completează username și parola.");
                return;
            }

            var passBlob = GetPassBlobByUsername(u);
            if (passBlob == null || !VerifyPassword(passBlob, p))
            {
                MessageBox.Show("Username sau parolă greșită.");
                return;
            }

            LoggedInUser = u;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void DoCreate()
        {
            var u = _tbCreateUser.Text.Trim();
            var p1 = _tbCreatePass.Text;
            var p2 = _tbCreatePass2.Text;

            if (string.IsNullOrWhiteSpace(u) || u.Length < 3)
            {
                MessageBox.Show("Username minim 3 caractere.");
                return;
            }
            if (string.IsNullOrEmpty(p1) || p1.Length < 4)
            {
                MessageBox.Show("Parola minim 4 caractere.");
                return;
            }
            if (p1 != p2)
            {
                MessageBox.Show("Parolele nu coincid.");
                return;
            }

            try
            {
                CreateUser(u, p1);
            }
            catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
            {
                MessageBox.Show("Username există deja.");
                return;
            }

            MessageBox.Show("Cont creat. Acum te poți loga.");

            _tbLoginUser.Text = u;
            _tbLoginPass.Text = "";
            _tbCreateUser.Text = "";
            _tbCreatePass.Text = "";
            _tbCreatePass2.Text = "";

            ShowTab(isLogin: true);
        }

        private void CloseCancel()
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        // ===== DB bootstrap users =====
        private static void EnsureDatabaseAndUsersTable()
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
IF OBJECT_ID(N'dbo.users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.users (
        user_id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_users PRIMARY KEY,
        username NVARCHAR(80) NOT NULL,
        pass_blob NVARCHAR(400) NOT NULL,
        created_at DATETIME2(0) NOT NULL CONSTRAINT DF_users_created DEFAULT SYSDATETIME()
    );
    CREATE UNIQUE INDEX UX_users_username ON dbo.users(username);
END;";
                cmd.ExecuteNonQuery();
            }
        }

        private static void SeedAdminIfEmpty()
        {
            using (var conn = new SqlConnection(CsDb))
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();
                cmd.CommandText = "SELECT COUNT(1) FROM dbo.users;";
                var count = (int)cmd.ExecuteScalar()!;
                if (count > 0) return;
            }

            CreateUser("admin", "admin123");
        }

        private static string? GetPassBlobByUsername(string username)
        {
            using var conn = new SqlConnection(CsDb);
            using var cmd = conn.CreateCommand();
            conn.Open();

            cmd.CommandText = "SELECT pass_blob FROM dbo.users WHERE username = @u;";
            cmd.Parameters.AddWithValue("@u", username);

            var obj = cmd.ExecuteScalar();
            return obj == null || obj == DBNull.Value ? null : (string)obj;
        }

        private static void CreateUser(string username, string password)
        {
            var blob = HashPasswordToBlob(password);

            using var conn = new SqlConnection(CsDb);
            using var cmd = conn.CreateCommand();
            conn.Open();

            cmd.CommandText = "INSERT INTO dbo.users(username, pass_blob) VALUES (@u, @b);";
            cmd.Parameters.AddWithValue("@u", username);
            cmd.Parameters.AddWithValue("@b", blob);
            cmd.ExecuteNonQuery();
        }

        // PBKDF2: Base64(salt[16] + hash[32])
        private static string HashPasswordToBlob(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            using var kdf = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
            byte[] hash = kdf.GetBytes(32);

            var all = new byte[48];
            Buffer.BlockCopy(salt, 0, all, 0, 16);
            Buffer.BlockCopy(hash, 0, all, 16, 32);

            return Convert.ToBase64String(all);
        }

        private static bool VerifyPassword(string passBlob, string password)
        {
            byte[] all;
            try { all = Convert.FromBase64String(passBlob); }
            catch { return false; }

            if (all.Length != 48) return false;

            var salt = new byte[16];
            var hash = new byte[32];
            Buffer.BlockCopy(all, 0, salt, 0, 16);
            Buffer.BlockCopy(all, 16, hash, 0, 32);

            using var kdf = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
            var computed = kdf.GetBytes(32);

            return CryptographicOperations.FixedTimeEquals(hash, computed);
        }

        // ===== UI helpers =====
        private static Button MakeTabButton(string text, int x, int y)
        {
            var b = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(150, 36),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Theme.White,
                BackColor = Theme.PrimaryDark,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = Theme.Border;
            return b;
        }

        private static Label MakeLabel(string text, int x, int y) =>
            new Label
            {
                Text = text,
                AutoSize = true,
                ForeColor = Theme.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Location = new Point(x, y)
            };

        private static TextBox MakeTextBox(int x, int y, int w, bool password)
        {
            return new TextBox
            {
                Location = new Point(x, y),
                Width = w,
                Font = new Font("Segoe UI", 11, FontStyle.Regular),
                UseSystemPasswordChar = password
            };
        }

        private static Button MakeActionButton(string text, int x, int y, Action onClick)
        {
            var b = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(140, 38),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Primary,
                ForeColor = Theme.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            b.FlatAppearance.BorderSize = 1;
            b.Click += (_, __) => onClick();
            return b;
        }

        private static Button MakeSecondaryButton(string text, int x, int y, Action onClick)
        {
            var b = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(140, 38),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.PrimaryDark,
                ForeColor = Theme.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            b.FlatAppearance.BorderSize = 1;
            b.Click += (_, __) => onClick();
            return b;
        }
    }
}
