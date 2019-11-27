﻿using Microsoft.Win32;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MyMailClient
{
    /// <summary>
    /// Логика взаимодействия для LetterWindow.xaml
    /// </summary>
    public partial class LetterWindow : Window
    {
        public LetterWindow()
        {
            InitializeComponent();
            showLetter();
        }

        private void showLetter()
        {
            try
            {
                // Заполнение полей из заголовка
                StringBuilder from_toStr = new StringBuilder("От " + CurrentData.curLetter.From.Mailboxes.First().Name
                    + " <" + CurrentData.curLetter.From.Mailboxes.First().Address + "> для ");
                //заполняем для кого
                foreach (MailboxAddress receiver in CurrentData.curLetter.To)
                    from_toStr.Append(receiver.Name + " <" + receiver.Address + ">, ");
                //удаляем запятую и пробел
                from_toStr.Remove(from_toStr.Length - 2, 2);
                if (CurrentData.curLetter.Cc.Count > 0)
                {
                    from_toStr.Append("; Копии: ");
                    foreach (MailboxAddress receiver in CurrentData.curLetter.Cc)
                        from_toStr.Append(receiver.Name + " <" + receiver.Address + ">, ");
                    from_toStr.Remove(from_toStr.Length - 2, 2);
                }
                lbl_send.Content = lbl_send.ToolTip = from_toStr;

                lbl_subj.Content = lbl_subj.ToolTip = CurrentData.curLetter.Subject;
                lbl_date.Content = lbl_date.ToolTip = CurrentData.curLetter.Date;
                string body = CurrentData.curLetter.HtmlBody ?? CurrentData.curLetter.TextBody;

                if (CurrentData.curLetter.Headers.Contains(Cryptography.ENCRYPTION_ID_HEADER))
                // Если письмо зашифровано
                {
                    List<CryptoKey> results = CurrentData.curAcc.Keys.Where(k => k.Id.Equals(CurrentData.curLetter.
                            Headers[Cryptography.ENCRYPTION_ID_HEADER]) && !k.PublicOnly).ToList();
                    if (results.Count > 0)
                    {
                        CryptoKey key = results.First();
                        // TODO: обработать неудачу
                        body = Cryptography.Decrypt(body, key);

                        lbl_encr.Content = lbl_encr.ToolTip =
                                "Расшифровано с помощью \"" + key + "\"";
                        lbl_encr.Foreground = Brushes.Green;
                    }
                    else
                    {
                        lbl_encr.Content = lbl_encr.ToolTip =
                                "Письмо зашифровано, ключ не найден";
                        lbl_encr.Foreground = Brushes.DarkRed;
                    }
                }
                else
                {
                    lbl_encr.Content = lbl_encr.ToolTip =
                            "Письмо не зашифровано";
                    lbl_encr.Foreground = Brushes.Black;
                }

                //if (message.Headers.Contains(Cryptography.SIGNATURE_ID_HEADER))
                //// Если письмо подписано
                //{
                //    List<CryptoKey> results = account.keys.Where(k => k.Id.Equals(message.
                //            Headers[Cryptography.SIGNATURE_ID_HEADER])).ToList();
                //    if (results.Count > 0)
                //    {
                //        CryptoKey key = results.First();
                //        if (Cryptography.Verify(body, message.Headers[Cryptography.SIGNATURE_HEADER], key))
                //        {
                //            signatureStatusLabel.Content = signatureStatusLabel.ToolTip =
                //                    "Верифицировано с помощью \"" + key + "\"";
                //            if (message.From.Mailboxes.First().Address.Equals(key.OwnerAddress))
                //                signatureStatusLabel.Foreground = Brushes.Green;
                //            else
                //            {
                //                signatureStatusLabel.Content = signatureStatusLabel.ToolTip +=
                //                        " (отправитель не совпадает)";
                //                signatureStatusLabel.Foreground = Brushes.DarkOrange;
                //            }
                //        }
                //        else
                //        {
                //            signatureStatusLabel.Content = signatureStatusLabel.ToolTip =
                //                    "Подпись распознана с помощью\"" + key +
                //                    "\", однако целостность письма нарушена";
                //            signatureStatusLabel.Foreground = Brushes.DarkRed;
                //        }
                //    }
                //    else
                //    {
                //        signatureStatusLabel.Content = signatureStatusLabel.ToolTip =
                //                "Письмо подписано, но нет подходящего ключа для верификации";
                //        signatureStatusLabel.Foreground = Brushes.Black;
                //    }
                //}
                //else
                //{
                //    signatureStatusLabel.Content = signatureStatusLabel.ToolTip =
                //            "Письмо не подписано";
                //    signatureStatusLabel.Foreground = Brushes.Black;
                //}

                // Отображение тела письма
                int index = body.IndexOf("<html>", StringComparison.OrdinalIgnoreCase);
                if (index < 0)
                    body = "<html><meta charset=\"" + Utility.HTML_CHARSET + "\"><body>" + body + "</body></html>";
                else
                    body = body.Insert(index + 6, "<meta charset=\"" + Utility.HTML_CHARSET + "\">");
                // TODO: превратить переносы строк в <br>, если письмо не HTML

                wbBrwsr.NavigateToString(body);

                if (CurrentData.curLetter.Headers.Contains(Cryptography.KEY_DELIVERY_HEADER))
                // Если это письмо - доставка ключа
                {
                    // Получить объект ключа
                    var tempparts = CurrentData.curLetter.HtmlBody.Split('$');

                    dynamic temp = new JavaScriptSerializer().DeserializeObject(tempparts[1]);

                    CurrentData.curKey = new CryptoKey(temp["PublicKey"], temp["PrivateKey"],
                        temp["Name"], temp["OwnerAddress"], temp["EncrOrSign"], temp["Id"], (DateTime)temp["DateTime"]);

                    if (CurrentData.curKey != null)
                    {
                        if (Utility.ShowConfirmation("Данное письмо содержит ключ. Вы хотите добавить его в библиотеку ключей?") == MessageBoxResult.Yes)
                        {
                            if (CurrentData.curAcc.AddKey())
                            {
                                CurrentData.curAcc.Srlz();
                                Utility.MsgBox("Ключ успешно добавлен", "Уведомление", this);
                            }
                            else
                            {
                                Utility.MsgBox("Такой ключ уже есть в библиотеке ключей", "Уведомление", this);
                            }
                        }
                    }
                }
                //else
                //// Если это письмо обыкновенное
                //{
                // Отображение прикреплений
                attachPanel.Items.Clear();
                foreach (MimeEntity attachment in CurrentData.curLetter.Attachments)
                    attachPanel.Items.Add(attachment);
                //}
                //replyBtn.IsEnabled = true;
            }
            catch (Exception ex)
            {
                Utility.MsgBox(ex.Message, "Ошибка", this);
            }

        }
        private void Chip_Click(object sender, RoutedEventArgs e)
        {
            MimeEntity attachment = (sender as FrameworkElement).DataContext as MimeEntity;
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = "Сохранить файл...";
            sfd.FileName = attachment.ContentDisposition?.FileName ?? attachment.ContentType.Name;
            if (sfd.ShowDialog(this).Value)
                Utility.SaveAttachment(attachment, sfd.FileName);
        }

        private void btn_reply_Click(object sender, RoutedEventArgs e)
        {

        }


    }
}
