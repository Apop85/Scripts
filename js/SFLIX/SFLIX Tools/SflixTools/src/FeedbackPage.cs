// Reiter "Lizenz" bzw. "Feedback" im Hauptfenster (seit 3.3).
//   Ohne bestätigten Schlüssel: Eingabe des Lizenzschlüssels ("Lizenz").
//   Mit bestätigtem Schlüssel: Feedbackformular mit Diagnose und Protokoll als Anhang ("Feedback"),
//   darunter "Lizenzschlüssel entfernen".
// Der Schlüssel steht in sflix_sys\license.js und gilt auch für STEFFLIX (LicenseStore).

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace Sflix
{
    public class FeedbackPage : TableLayoutPanel
    {
        // Lizenzstatus hat sich geändert (Reiter umbenennen)
        public Action StatusChanged;
        // Anhänge
        public Func<string> SessionLog;
        public Func<string> Diagnosis;

        readonly TableLayoutPanel licensePanel, feedbackPanel;
        readonly TextBox keyBox, nameBox, messageBox;
        readonly Button saveButton, retryButton, sendButton;
        readonly Label keyStatus, sendStatus;
        readonly DarkCheckBox attachDiagnosis, attachLog;
        readonly LinkLabel removeLink;
        bool busy;

        static readonly Color ErrorColor = Color.FromArgb(0xff, 0x6b, 0x6b);
        static readonly Color SuccessColor = Color.FromArgb(0x5e, 0xd3, 0x8a);

        public FeedbackPage()
        {
            ColumnCount = 1;
            ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            AutoSize = true;
            Dock = DockStyle.Fill;
            BackColor = Theme.Back;
            Margin = new Padding(0);

            // --- Lizenz eingeben ---
            licensePanel = Card("Lizenzschlüssel");
            licensePanel.Controls.Add(Hint(
                "Der Lizenzschlüssel schaltet das Feedback im Updater und in STEFFLIX frei. Er gilt für diese Installation " +
                "in allen Browsern. Den ganzen Schlüssel einfügen (er beginnt mit SFX2), Zeilenumbrüche spielen keine Rolle. " +
                "Beim Speichern wird eine Bestätigung mit der Kennung dieser Installation verschickt."));
            keyBox = Theme.MakeTextBox();
            keyBox.Multiline = true;
            keyBox.Height = Theme.S(64);
            keyBox.Dock = DockStyle.Fill;
            keyBox.Font = new Font("Consolas", 10f);
            keyBox.Margin = new Padding(0, Theme.S(8), 0, Theme.S(8));
            licensePanel.Controls.Add(keyBox);
            FlowLayoutPanel keyButtons = Row();
            saveButton = Theme.MakeButton("Speichern", true);
            saveButton.Click += delegate { SaveKey(); };
            retryButton = Theme.MakeButton("Erneut versuchen", false);
            retryButton.Margin = new Padding(Theme.S(8), 0, 0, 0);
            retryButton.Click += delegate { RetryConfirm(); };
            keyButtons.Controls.Add(saveButton);
            keyButtons.Controls.Add(retryButton);
            licensePanel.Controls.Add(keyButtons);
            keyStatus = Hint("");
            keyStatus.Margin = new Padding(0, Theme.S(8), 0, 0);
            licensePanel.Controls.Add(keyStatus);

            // --- Feedback ---
            feedbackPanel = Card("Feedback und Fehler melden");
            feedbackPanel.Controls.Add(Hint("Fragen, Wünsche oder Fehler direkt an den Entwickler schicken. Mit Diagnose und Protokoll " +
                                            "lässt sich ein Fehler meist schnell eingrenzen."));
            nameBox = Theme.MakeTextBox();
            nameBox.Dock = DockStyle.Fill;
            feedbackPanel.Controls.Add(Labeled("Name:", nameBox));
            messageBox = Theme.MakeTextBox();
            messageBox.Multiline = true;
            messageBox.ScrollBars = ScrollBars.Vertical;
            messageBox.AcceptsReturn = true;
            messageBox.Height = Theme.S(150);
            messageBox.Dock = DockStyle.Fill;
            messageBox.Margin = new Padding(0, Theme.S(4), 0, Theme.S(6));
            Label messageLabel = Theme.MakeLabel("Nachricht:", false);
            messageLabel.Margin = new Padding(0, Theme.S(8), 0, 0);
            feedbackPanel.Controls.Add(messageLabel);
            feedbackPanel.Controls.Add(messageBox);
            attachDiagnosis = Switch("Diagnose anhängen (Versionen, Windows, ffmpeg, Installationsordner)");
            attachDiagnosis.Checked = true;
            attachLog = Switch("Protokoll dieser Sitzung anhängen");
            attachLog.Checked = true;
            feedbackPanel.Controls.Add(attachDiagnosis);
            feedbackPanel.Controls.Add(attachLog);
            FlowLayoutPanel sendRow = Row();
            sendRow.Margin = new Padding(0, Theme.S(8), 0, 0);
            sendButton = Theme.MakeButton("Senden", true);
            sendButton.Click += delegate { Send(); };
            sendRow.Controls.Add(sendButton);
            feedbackPanel.Controls.Add(sendRow);
            sendStatus = Hint("");
            sendStatus.Margin = new Padding(0, Theme.S(8), 0, 0);
            feedbackPanel.Controls.Add(sendStatus);
            removeLink = new LinkLabel();
            removeLink.Text = "Lizenzschlüssel entfernen";
            removeLink.AutoSize = true;
            removeLink.LinkColor = Theme.Muted;
            removeLink.ActiveLinkColor = Theme.Text;
            removeLink.Margin = new Padding(0, Theme.S(14), 0, 0);
            removeLink.LinkClicked += delegate { RemoveKey(); };
            feedbackPanel.Controls.Add(removeLink);

            Controls.Add(licensePanel);
            Controls.Add(feedbackPanel);
            RefreshState();
        }

        public LicenseStatus Status { get; private set; }

        // Ansicht passend zum Lizenzstatus
        public void RefreshState()
        {
            Status = LicenseStore.Status(App.Root);
            bool active = Status == LicenseStatus.Active;
            licensePanel.Visible = !active;
            feedbackPanel.Visible = active;
            retryButton.Visible = Status == LicenseStatus.Pending;
            if (!busy)
            {
                if (Status == LicenseStatus.Pending)
                    SetStatus(keyStatus, "Der Schlüssel ist gespeichert, die Bestätigung steht noch aus (keine Verbindung?). " +
                              "Das Feedback ist verfügbar, sobald sie verschickt wurde.", false);
                else if (Status == LicenseStatus.Invalid)
                    SetStatus(keyStatus, "Der hinterlegte Schlüssel ist ungültig. Bitte einen gültigen Schlüssel eingeben.", true);
            }
            if (StatusChanged != null) StatusChanged();
        }

        void SaveKey()
        {
            string key = keyBox.Text.Trim();
            string error;
            if (LicenseKey.Decode(key, out error) == null) { SetStatus(keyStatus, error, true); return; }
            RunBusy(saveButton, keyStatus, "Schlüssel wird geprüft und bestätigt …", delegate
            {
                string message;
                bool ok = LicenseStore.Save(key, out message);
                return new object[] { ok, message };
            }, delegate(object[] r)
            {
                bool ok = (bool)r[0];
                SetStatus(keyStatus, (string)r[1], !ok);
                if (ok) keyBox.Text = "";
                RefreshState();
                if (ok && Status == LicenseStatus.Active) SetStatus(sendStatus, (string)r[1], false);
            });
        }

        void RetryConfirm()
        {
            RunBusy(retryButton, keyStatus, "Bestätigung wird gesendet …", delegate
            {
                return new object[] { LicenseStore.ConfirmPending() };
            }, delegate(object[] r)
            {
                string note = (string)r[0];
                RefreshState();
                if (Status == LicenseStatus.Active) SetStatus(sendStatus, note ?? "Lizenz bestätigt.", false);
                else SetStatus(keyStatus, note ?? "Die Bestätigung konnte noch nicht gesendet werden. Bitte Internetverbindung prüfen.", true);
            });
        }

        void Send()
        {
            string sender = nameBox.Text.Trim(), text = messageBox.Text.Trim();
            if (sender.Length == 0 || text.Length == 0)
            {
                SetStatus(sendStatus, "Bitte " + (sender.Length == 0 && text.Length == 0 ? "einen Namen und eine Nachricht" :
                                                  sender.Length == 0 ? "einen Namen" : "eine Nachricht") + " eingeben.", true);
                return;
            }
            // Anhänge auf dem UI-Thread zusammenstellen
            List<Discord.Attachment> files = new List<Discord.Attachment>();
            bool withDiagnosis = attachDiagnosis.Checked;
            if (attachLog.Checked && SessionLog != null) files.Add(new Discord.Attachment("protokoll.txt", SessionLog()));
            RunBusy(sendButton, sendStatus, "Nachricht wird gesendet …", delegate
            {
                // Diagnose im Hintergrund (prüft u.a. den Encoder von ffmpeg)
                if (withDiagnosis && Diagnosis != null) files.Insert(0, new Discord.Attachment("diagnose.txt", Diagnosis()));
                string message;
                bool ok = LicenseStore.SendFeedback(sender, text, files, out message);
                return new object[] { ok, message };
            }, delegate(object[] r)
            {
                bool ok = (bool)r[0];
                SetStatus(sendStatus, (string)r[1], !ok);
                if (ok) messageBox.Text = "";
                RefreshState();   // z.B. Webhook gelöscht: Status bleibt, Meldung erklärt es
            });
        }

        void RemoveKey()
        {
            if (busy) return;
            if (MessageBox.Show(FindForm(), "Lizenzschlüssel entfernen? Das Feedback ist danach im Updater und in STEFFLIX nicht mehr verfügbar.",
                                App.Title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            RunBusy(null, sendStatus, "Abmeldung wird gesendet …", delegate
            {
                string message;
                bool ok = LicenseStore.Remove(false, true, out message);
                return new object[] { ok, message };
            }, delegate(object[] r)
            {
                if (!(bool)r[0])
                {
                    // Ohne Verbindung nachfragen
                    if (MessageBox.Show(FindForm(), (string)r[1] + ".\n\nTrotzdem entfernen?", App.Title,
                                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    {
                        SetStatus(sendStatus, "Der Lizenzschlüssel wurde nicht entfernt.", false);
                        return;
                    }
                    string message;
                    LicenseStore.Remove(true, true, out message);
                    r[1] = message;
                }
                RefreshState();
                SetStatus(keyStatus, (string)r[1], false);
            });
        }

        // Arbeit im Hintergrund, danach Ergebnis auf dem UI-Thread
        void RunBusy(Button button, Label status, string text, Func<object[]> work, Action<object[]> done)
        {
            if (busy) return;
            busy = true;
            if (button != null) button.Enabled = false;
            SetStatus(status, text, false);
            Cursor = Cursors.WaitCursor;
            ThreadPool.QueueUserWorkItem(delegate
            {
                object[] result;
                try { result = work(); }
                catch (Exception e) { result = new object[] { false, "Fehler: " + e.Message }; }
                if (IsDisposed) return;
                BeginInvoke((Action)delegate
                {
                    busy = false;
                    if (button != null) button.Enabled = true;
                    Cursor = Cursors.Default;
                    done(result);
                });
            });
        }

        static void SetStatus(Label l, string text, bool error)
        {
            l.Text = text ?? "";
            l.ForeColor = error ? ErrorColor : (text != null && (text.StartsWith("Danke") || text.StartsWith("Lizenz gespeichert und") || text.StartsWith("Lizenz bestätigt"))
                ? SuccessColor : Theme.Muted);
        }

        // --- Bausteine ---

        static TableLayoutPanel Card(string title)
        {
            TableLayoutPanel card = new TableLayoutPanel();
            card.Dock = DockStyle.Fill;
            card.AutoSize = true;
            card.ColumnCount = 1;
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            card.BackColor = Theme.Card;
            card.Padding = new Padding(Theme.S(16), Theme.S(12), Theme.S(16), Theme.S(14));
            card.Margin = new Padding(0, 0, 0, Theme.S(8));
            Label t = Theme.MakeLabel(title, false);
            t.Font = new Font("Segoe UI Semibold", 10.5f);
            t.Margin = new Padding(0, 0, 0, Theme.S(4));
            card.Controls.Add(t);
            return card;
        }

        static Label Hint(string text)
        {
            Label l = Theme.MakeLabel(text, true);
            l.MaximumSize = new Size(Theme.S(800), 0);
            l.Margin = new Padding(0);
            return l;
        }

        static FlowLayoutPanel Row()
        {
            FlowLayoutPanel row = new FlowLayoutPanel();
            row.AutoSize = true;
            row.WrapContents = false;
            row.Margin = new Padding(0);
            row.BackColor = Color.Transparent;
            return row;
        }

        static DarkCheckBox Switch(string text)
        {
            DarkCheckBox c = new DarkCheckBox();
            c.Text = text;
            c.ForeColor = Theme.Text;
            c.Switch = true;
            c.Margin = new Padding(0, Theme.S(2), 0, Theme.S(2));
            c.FitText();
            return c;
        }

        static Control Labeled(string label, Control box)
        {
            TableLayoutPanel row = new TableLayoutPanel();
            row.Dock = DockStyle.Fill;
            row.AutoSize = true;
            row.ColumnCount = 2;
            row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            row.Margin = new Padding(0, Theme.S(10), 0, 0);
            Label l = Theme.MakeLabel(label, false);
            l.Margin = new Padding(0, Theme.S(4), Theme.S(8), 0);
            box.Margin = new Padding(0);
            row.Controls.Add(l, 0, 0);
            row.Controls.Add(box, 1, 0);
            return row;
        }
    }
}
