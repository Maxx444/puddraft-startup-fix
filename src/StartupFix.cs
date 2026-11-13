// PUDDraft v0.96 startup-crash fix (personal patch, 2026-11-13)
// Fixes "Access violation at address 00450E18. Read of address 0000001C"
// when PUDDraft starts maximized. Put the exe next to PUDDraft.exe and run it.
// It patches PUDDraft.exe in place after backing it up to backup\PUDDraft.exe,
// or puts the original back from that backup.
// Command line (no window, exit code 0 = ok):  PUDDraft-StartupFix.exe --patch | --revert
// Build (uses only Windows' own .NET Framework compiler):
//   %WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe /nologo /target:winexe
//     /win32manifest:app.manifest /out:PUDDraft-StartupFix.exe StartupFix.cs
// Personal use only; do not redistribute patched exes.

using System;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Forms;

static class StartupFix
{
    const string ORIG  = "526745751e70b142a0bac9138b47833d7aaf7b9e3ef9d1d242b3eeb4e1aa4ddb";
    const string FIXED = "ebacd1b52ffe1be0ca5c758858b4aedff025474dd8cfd6c040f6ad9bc0639b74";

    // file offset, original bytes, new bytes
    static readonly int[] Off = { 0x00200, 0x50218, 0x59830 };
    static readonly string[] Old = {
        "24",
        "0fb7401c8bb3a05a0100",
        "0000000000000000000000000000000000000000000000" };
    static readonly string[] New = {
        "50",
        "e9139600009090909090",
        "85c00f84ab6affff0fb7401c8bb3a05a0100e9db69ffff" };

    static string Dir, Exe, BakDir, Bak;
    static Action<string> Say = delegate(string s) { };

    static string Sha(byte[] data)
    {
        using (SHA256 h = SHA256.Create())
            return BitConverter.ToString(h.ComputeHash(data)).Replace("-", "").ToLowerInvariant();
    }
    static string ShaFile(string p) { return File.Exists(p) ? Sha(File.ReadAllBytes(p)) : null; }

    static byte[] Hex(string h)
    {
        byte[] b = new byte[h.Length / 2];
        for (int i = 0; i < b.Length; i++) b[i] = Convert.ToByte(h.Substring(i * 2, 2), 16);
        return b;
    }

    static string Describe(string h)
    {
        if (h == null) return "none";
        if (h == ORIG) return "original v0.96 (not patched)";
        if (h == FIXED) return "PATCHED (startup fix applied)";
        return "unknown version - this patch does not fit it";
    }

    static bool DoPatch()
    {
        string h = ShaFile(Exe);
        if (h == FIXED) { Say("PUDDraft.exe is already patched. Nothing to do."); return true; }
        if (h != ORIG) { Say("PUDDraft.exe is missing or not the expected v0.96 file. Not patching."); return false; }

        // 1) back up the original
        string bh = ShaFile(Bak);
        if (bh == null)
        {
            Directory.CreateDirectory(BakDir);
            File.Copy(Exe, Bak);
            if (ShaFile(Bak) != ORIG) { Say("Backup failed. Not patching."); return false; }
            Say("Backed up original to backup\\PUDDraft.exe");
        }
        else if (bh == ORIG) Say("Original backup already present in backup\\.");
        else { Say("backup\\PUDDraft.exe exists but is not the original. Move it away first. Not patching."); return false; }

        // 2) patch in memory, check, then write
        byte[] data = File.ReadAllBytes(Exe);
        for (int k = 0; k < Off.Length; k++)
        {
            byte[] o = Hex(Old[k]), n = Hex(New[k]);
            for (int i = 0; i < o.Length; i++)
                if (data[Off[k] + i] != o[i]) { Say(string.Format("Unexpected bytes at 0x{0:X}. Not patching.", Off[k])); return false; }
            Array.Copy(n, 0, data, Off[k], n.Length);
        }
        if (Sha(data) != FIXED) { Say("Patched result is wrong. Not writing."); return false; }
        try { File.WriteAllBytes(Exe, data); }
        catch (Exception) { Say("Could not write PUDDraft.exe. Close PUDDraft and try again."); return false; }
        if (ShaFile(Exe) == FIXED) { Say("Done: PUDDraft.exe is patched."); return true; }
        File.Copy(Bak, Exe, true);
        Say("Patch check failed; original restored from backup.");
        return false;
    }

    static bool DoRevert()
    {
        if (ShaFile(Exe) == ORIG) { Say("PUDDraft.exe is already the original. Nothing to do."); return true; }
        if (ShaFile(Bak) != ORIG) { Say("No good original backup in backup\\PUDDraft.exe. Cannot revert."); return false; }
        try { File.Copy(Bak, Exe, true); }
        catch (Exception) { Say("Could not write PUDDraft.exe. Close PUDDraft and try again."); return false; }
        if (ShaFile(Exe) == ORIG) { Say("Done: original PUDDraft.exe restored. (Backup kept.)"); return true; }
        Say("Revert check failed.");
        return false;
    }

    [STAThread]
    static int Main(string[] args)
    {
        Dir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
        Exe = Path.Combine(Dir, "PUDDraft.exe");
        BakDir = Path.Combine(Dir, "backup");
        Bak = Path.Combine(BakDir, "PUDDraft.exe");

        if (args.Length > 0)
        {
            string log = "";
            Say = delegate(string s) { log += s + Environment.NewLine; };
            bool ok = false;
            try
            {
                if (args[0] == "--patch") ok = DoPatch();
                else if (args[0] == "--revert") ok = DoRevert();
                else Say("Unknown option. Use --patch or --revert.");
            }
            catch (Exception e) { Say("Error: " + e.Message); }
            try { File.WriteAllText(Path.Combine(Dir, "PUDDraft-StartupFix.log"), log); } catch (Exception) { }
            return ok ? 0 : 1;
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(BuildForm());
        return 0;
    }

    static Form BuildForm()
    {
        Form form = new Form();
        form.Text = "PUDDraft v0.96 startup fix";
        form.Font = new Font("Segoe UI", 9f);
        form.ClientSize = new Size(460, 330);
        form.FormBorderStyle = FormBorderStyle.FixedDialog;
        form.MaximizeBox = false;
        form.StartPosition = FormStartPosition.CenterScreen;
        try { form.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch (Exception) { }

        Label intro = new Label();
        intro.Location = new Point(12, 10); intro.Size = new Size(436, 36);
        intro.Text = "Fixes the \"Access violation at address 00450E18\" crash when PUDDraft starts maximized. The original is backed up to the backup folder first.";
        form.Controls.Add(intro);

        GroupBox grp = new GroupBox();
        grp.Text = "Status"; grp.Location = new Point(12, 50); grp.Size = new Size(436, 70);
        form.Controls.Add(grp);
        Label lblExe = new Label(); lblExe.Location = new Point(10, 20); lblExe.Size = new Size(416, 20);
        Label lblBak = new Label(); lblBak.Location = new Point(10, 42); lblBak.Size = new Size(416, 20);
        grp.Controls.Add(lblExe); grp.Controls.Add(lblBak);

        Button btnPatch = new Button();
        btnPatch.Text = "Patch PUDDraft"; btnPatch.Location = new Point(12, 130); btnPatch.Size = new Size(140, 30);
        Button btnRevert = new Button();
        btnRevert.Text = "Revert to original"; btnRevert.Location = new Point(160, 130); btnRevert.Size = new Size(140, 30);
        Button btnClose = new Button();
        btnClose.Text = "Close"; btnClose.Location = new Point(348, 130); btnClose.Size = new Size(100, 30);
        btnClose.Click += delegate { form.Close(); };
        form.CancelButton = btnClose;
        form.Controls.Add(btnPatch); form.Controls.Add(btnRevert); form.Controls.Add(btnClose);

        TextBox log = new TextBox();
        log.Multiline = true; log.ReadOnly = true; log.ScrollBars = ScrollBars.Vertical;
        log.Location = new Point(12, 170); log.Size = new Size(436, 120);
        form.Controls.Add(log);

        Label folder = new Label();
        folder.Location = new Point(12, 298); folder.Size = new Size(436, 26);
        folder.ForeColor = Color.Gray; folder.Text = "Folder: " + Dir;
        form.Controls.Add(folder);

        Say = delegate(string s) { log.AppendText(s + Environment.NewLine); };

        Action refresh = delegate
        {
            string h = ShaFile(Exe), b = ShaFile(Bak);
            lblExe.Text = "PUDDraft.exe:  " + (h == null ? "not found in this folder" : Describe(h));
            lblBak.Text = "Backup:            " + Describe(b);
            lblExe.ForeColor = h == FIXED ? Color.DarkGreen : (h == ORIG ? SystemColors.ControlText : Color.DarkRed);
            btnPatch.Enabled = h == ORIG;
            btnRevert.Enabled = h != ORIG && b == ORIG;
        };

        btnPatch.Click += delegate
        {
            try { DoPatch(); } catch (Exception e) { Say("Error: " + e.Message); }
            refresh();
        };
        btnRevert.Click += delegate
        {
            try { DoRevert(); } catch (Exception e) { Say("Error: " + e.Message); }
            refresh();
        };

        refresh();
        if (!File.Exists(Exe)) Say("Put this program in the same folder as PUDDraft.exe and run it again.");
        return form;
    }
}
