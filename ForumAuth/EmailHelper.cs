using System;
using System.Net.Mail;
using System.Web;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Logging;
using Umbraco.Core.Logging.Serilog;
using Umbraco.Web;

namespace Forums
{
    /// <summary>
    /// does the sending of email, for the MemberAuth
    /// assumes your umbraco install can already send email.
    /// </summary>
    public class ForumEmailHelper
    {
        private SerilogLogger logger = new SerilogLogger(new LoggerConfiguration()
            .MinimalConfiguration()
            .ReadFromConfigFile()
            .ReadFromUserConfigFile()
            .MinimumLevel.ControlledBy(new LoggingLevelSwitch { MinimumLevel = LogEventLevel.Information }));

        public void SendResetPassword(UmbracoHelper umbraco, string email, string guid)
        {
            try
            {
                //logger.Info<EmailHelper>("Send Reset: {0} {1}", email, guid);

                string from = Current.Configs.Settings().Content.NotificationEmailAddress;
                string baseURL = HttpContext.Current.Request.Url.AbsoluteUri.Replace(HttpContext.Current.Request.Url.AbsolutePath, string.Empty);
                var resetUrl = baseURL + Forums.ForumAuthConstants.ResetUrl + "?resetGUID=" + guid;

                var messageBody = umbraco.GetDictionaryValue("Forum.ResetBody",$@"<h2>Password Reset</h2>
            <p>we have received a request to reset your password</p>
            <p><a href='{resetUrl}'>Reset your password</a></p>");

                MailMessage message = new MailMessage(from, email)
                {
                    Subject = umbraco.GetDictionaryValue("Forum.ResetSubject", "Reset your password"),
                    IsBodyHtml = true,
                    Body = messageBody
                };

                SmtpClient client = new SmtpClient();
                client.Send(message);
            }
            catch (Exception ex)
            {
                logger.Error<ForumEmailHelper>("Error {0}",ex.Message);
            }
        }

        public void SendVerifyAccount(UmbracoHelper umbraco, string email, string guid)
        {
            try
            {
                //logger.Info<ForumEmailHelper>("Send Verify: {0} {1}", email, guid);

                string from = Current.Configs.Settings().Content.NotificationEmailAddress;
                string baseURL = HttpContext.Current.Request.Url.AbsoluteUri.Replace(HttpContext.Current.Request.Url.AbsolutePath, string.Empty);
                var resetUrl = baseURL + Forums.ForumAuthConstants.VerifyUrl + "?verifyGUID=" + guid;

                var messageBody = umbraco.GetDictionaryValue("Forum.VerifyBody",$@"<h2>Verifiy your account</h2>
            <p>in order to use your account, you first need to verifiy your email address using the link below.</p>
            <p><a href='{resetUrl}'>Verify your account</a></p>");


                MailMessage message = new MailMessage(from, email)
                {
                    Subject = umbraco.GetDictionaryValue("Forum.VerifySubject", "Verifiy your account"),
                    IsBodyHtml = true,
                    Body = messageBody
                };

                using (var client = new SmtpClient())
                {
                    client.Send(message);// Your code here.
                }
 
            }
            catch (Exception ex)
            {
                logger.Error<ForumEmailHelper>("Problems sending verify email: {0}", ex.ToString());
            }
                
        }
    }
}