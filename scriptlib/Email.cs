using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Mail;

using Amazon;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;

namespace metascript
{
    public static class Email
    {
        public static bool WillSendEmail
        {
            get
            {
                EnsureInit();
                return sm_bSendMail;
            }
        }

        public static bool IsEmailValid(string email)
        {
            try
            {
                var emailAddress = new MailAddress(email);
                return emailAddress.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public static async Task SendEmailAsync(HttpState state, string toEmail, string toName, string subject, string textBody, string htmlBody)
        {
            EnsureInit();

            await WebUtils.LogInfoAsync(state, $"SendEmail: {toEmail} - {subject}");

            if (!IsEmailValid(toEmail))
                throw new UserException($"Sorry, that's an invalid email address: {toEmail}");
            MailAddress toAddress = new MailAddress(toEmail, toName);

            if (!sm_bSendMail)
            {
                await WebUtils.LogTraceAsync(state, "Not sending email, so this is a no-op");
                return;
            }

            Message msg = new Message();
            msg.Subject = new Content(subject);
            if (textBody != null && htmlBody != null)
            {
                msg.Body =
                    new Body
                    {
                        Html = new Content
                        {
                            Charset = "UTF-8",
                            Data = htmlBody
                        },
                        Text = new Content
                        {
                            Charset = "UTF-8",
                            Data = textBody
                        }
                    };
            }
            else if (htmlBody != null)
            {
                msg.Body =
                    new Body
                    {
                        Html = new Content
                        {
                            Charset = "UTF-8",
                            Data = htmlBody
                        }
                    };
            }
            else if (textBody != null)
            {
                msg.Body = new Body(new Content() { Charset = "UTF-8", Data = textBody });
            }
            else
                throw new MException("Invalid email request; no text or html to send");

            using (var client = new AmazonSimpleEmailServiceClient(sm_accessKey, sm_secretKey, sm_endpoint))
            {
                var sendRequest = new SendEmailRequest
                {
                    Source = sm_fromEmail,
                    Destination = new Destination
                    {
                        ToAddresses =
                        new List<string> { toAddress.ToString() }
                    },
                    Message = msg
                };

                var response = await client.SendEmailAsync(sendRequest);
                if ((int)response.HttpStatusCode / 100 != 2)
                    throw new Exception($"SendEmail failed: {toEmail} - {subject} - Status Code: {response.HttpStatusCode}");
            }
        }

        private static void EnsureInit()
        {
            if (sm_bInitted)
                return;
            lock (sm_initLock)
            {
                if (sm_bInitted)
                    return;

                sm_bSendMail = bool.Parse(MUtils.GetAppSetting("SendEmail"));

                if (sm_bSendMail)
                {
                    MailAddress fromAddress = 
                        new MailAddress
                        (
                            MUtils.GetAppSetting("EmailFromAddress"),
                            MUtils.GetAppSetting("EmailFromName")
                        );
                    if (!IsEmailValid(fromAddress.Address))
                        throw new MException($"Invalid from email address: {fromAddress}");
                    sm_fromEmail = fromAddress.ToString();

                    string serviceRegion = MUtils.GetAppSetting("EmailServiceRegion");
                    sm_endpoint = RegionEndpoint.GetBySystemName(serviceRegion);

                    sm_accessKey = MUtils.GetAppSetting("EmailAccessKey");
                    sm_secretKey = MUtils.GetAppSetting("EmailSecretKey");
                }

                sm_bInitted = true;
            }
        }
        private static bool sm_bInitted = false;
        private static object sm_initLock = new object();

        private static bool sm_bSendMail;

        private static string sm_fromEmail;

        private static RegionEndpoint sm_endpoint;
        private static string sm_accessKey;
        private static string sm_secretKey;
    }
}
