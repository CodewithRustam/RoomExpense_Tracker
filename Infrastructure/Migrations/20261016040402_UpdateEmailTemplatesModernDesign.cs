using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEmailTemplatesModernDesign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var passwordResetHtml = @"<!DOCTYPE html>
<html lang='en' xmlns='http://www.w3.org/1999/xhtml'>
<head>
  <meta charset='UTF-8'>
  <meta name='viewport' content='width=device-width, initial-scale=1.0'>
  <meta http-equiv='X-UA-Compatible' content='IE=edge'>
  <title>Reset Your Password</title>
  <!--[if mso]>
  <style type='text/css'>
    body, table, td, a { font-family: Arial, sans-serif !important; }
  </style>
  <![endif]-->
  <style type='text/css'>
    body { margin:0; padding:0; -webkit-text-size-adjust:100%; -ms-text-size-adjust:100%; }
    table, td { mso-table-lspace:0pt; mso-table-rspace:0pt; }
    img { -ms-interpolation-mode:bicubic; border:0; line-height:100%; outline:none; text-decoration:none; }
    @media (prefers-color-scheme: dark) {
      .email-bg { background-color: #0b0f19 !important; }
      .email-card { background-color: #111827 !important; border-color: #1f2937 !important; }
      .text-title { color: #f9fafb !important; }
      .text-body { color: #cbd5e1 !important; }
      .text-muted { color: #94a3b8 !important; }
      .info-box { background-color: #1e293b !important; border-color: #334155 !important; }
      .info-box-text { color: #cbd5e1 !important; }
      .link-box { background-color: #182234 !important; border-color: #334155 !important; color: #a5b4fc !important; }
      .footer-text { color: #64748b !important; }
      .divider { border-color: #1f2937 !important; }
    }
    @media only screen and (max-width: 600px) {
      .container { width: 100% !important; padding: 12px !important; }
      .card-content { padding: 28px 20px !important; }
      .btn { display: block !important; width: 100% !important; text-align: center !important; }
    }
  </style>
</head>
<body class='email-bg' style='margin: 0; padding: 0; background-color: #f1f5f9; font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif; color: #334155;'>
  <!-- Preheader text for inbox preview -->
  <div style='display:none; font-size:1px; color:#f1f5f9; line-height:1px; max-height:0px; max-width:0px; opacity:0; overflow:hidden;'>
    Reset your SplitX password. This secure link expires in 60 minutes.
  </div>

  <table role='presentation' border='0' cellpadding='0' cellspacing='0' width='100%' class='email-bg' style='background-color: #f1f5f9;'>
    <tr>
      <td align='center' style='padding: 40px 16px;'>
        <table role='presentation' class='container' border='0' cellpadding='0' cellspacing='0' width='100%' style='max-width: 540px;'>
          
          <!-- Logo & Header -->
          <tr>
            <td align='center' style='padding-bottom: 24px;'>
              <table role='presentation' border='0' cellpadding='0' cellspacing='0'>
                <tr>
                  <td align='center' style='vertical-align: middle;'>
                    <img src='https://splitx-exp.netlify.app/assets/donut-chart.png' alt='SplitX Logo' width='40' height='40' style='display: block; border-radius: 8px;' />
                  </td>
                  <td style='vertical-align: middle; padding-left: 10px;'>
                    <span style='font-size: 22px; font-weight: 800; color: #0f172a; letter-spacing: -0.5px; font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, sans-serif;'>
                      Split<span style='color: #7044ff;'>X</span>
                    </span>
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- Main Card -->
          <tr>
            <td>
              <table role='presentation' class='email-card' border='0' cellpadding='0' cellspacing='0' width='100%' style='background-color: #ffffff; border-radius: 16px; border: 1px solid #e2e8f0; box-shadow: 0 4px 16px rgba(15, 23, 42, 0.04); overflow: hidden;'>
                
                <!-- Decorative Top Border Accent -->
                <tr>
                  <td height='4' style='background: linear-gradient(90deg, #7044ff 0%, #a855f7 100%); line-height: 4px; font-size: 4px;'>&nbsp;</td>
                </tr>

                <tr>
                  <td class='card-content' style='padding: 36px 36px 32px 36px;'>
                    
                    <!-- Icon Badge -->
                    <div style='text-align: center; margin-bottom: 20px;'>
                      <div style='display: inline-block; width: 56px; height: 56px; background-color: #f3f0ff; border-radius: 50%; line-height: 56px; text-align: center; font-size: 26px;'>
                        🔐
                      </div>
                    </div>

                    <!-- Title -->
                    <h1 class='text-title' style='margin: 0 0 10px 0; font-size: 22px; font-weight: 700; color: #0f172a; text-align: center; letter-spacing: -0.3px;'>
                      Password Reset Request
                    </h1>

                    <!-- Greeting -->
                    <p class='text-body' style='margin: 0 0 16px 0; font-size: 15px; line-height: 1.6; color: #475569; text-align: center;'>
                      Hello <strong style='color: #0f172a;'>{{MemberName}}</strong>,
                    </p>

                    <!-- Explanation -->
                    <p class='text-body' style='margin: 0 0 28px 0; font-size: 15px; line-height: 1.6; color: #475569; text-align: center;'>
                      We received a request to reset the password for your SplitX account. Click the button below to choose a new password.
                    </p>

                    <!-- CTA Button -->
                    <table role='presentation' border='0' cellpadding='0' cellspacing='0' width='100%'>
                      <tr>
                        <td align='center'>
                          <a href='{{ResetLink}}' class='btn' target='_blank' style='background-color: #7044ff; color: #ffffff !important; padding: 14px 32px; font-weight: 600; font-size: 15px; border-radius: 10px; display: inline-block; text-decoration: none; box-shadow: 0 4px 12px rgba(112, 68, 255, 0.28); letter-spacing: 0.2px;'>
                            Reset Password &rarr;
                          </a>
                        </td>
                      </tr>
                    </table>

                    <!-- Expiration Notice -->
                    <div style='text-align: center; margin-top: 24px;'>
                      <span style='display: inline-block; background-color: #fef3c7; color: #92400e; font-size: 13px; font-weight: 600; padding: 5px 12px; border-radius: 20px;'>
                        ⏰ Valid for 1 hour
                      </span>
                    </div>

                    <!-- Fallback Link Section -->
                    <div class='info-box' style='margin-top: 28px; padding: 14px 16px; background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 10px;'>
                      <p class='text-muted' style='margin: 0 0 6px 0; font-size: 12px; color: #64748b; font-weight: 600;'>
                        Or copy and paste this link into your browser:
                      </p>
                      <p class='link-box' style='margin: 0; font-size: 12px; color: #7044ff; word-break: break-all; font-family: ui-monospace, Menlo, Consolas, monospace; line-height: 1.4;'>
                        {{ResetLink}}
                      </p>
                    </div>

                    <!-- Divider -->
                    <hr class='divider' style='border: none; border-top: 1px solid #f1f5f9; margin: 28px 0 20px 0;' />

                    <!-- Security Note -->
                    <p class='text-muted' style='margin: 0; font-size: 13px; line-height: 1.5; color: #64748b; text-align: center;'>
                      If you didn't request a password reset, you can safely ignore this email. Your password won't change until you set a new one.
                    </p>

                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- Footer -->
          <tr>
            <td align='center' style='padding-top: 24px;'>
              <p class='footer-text' style='margin: 0 0 6px 0; font-size: 13px; color: #94a3b8; font-weight: 600;'>
                SplitX &bull; Split expenses simply & transparently
              </p>
              <p class='footer-text' style='margin: 0; font-size: 12px; color: #94a3b8; line-height: 1.4;'>
                This is an automated security notification. Please do not reply directly to this email.
              </p>
            </td>
          </tr>

        </table>
      </td>
    </tr>
  </table>
</body>
</html>";

            var settlementHtml = @"<!DOCTYPE html>
<html lang='en' xmlns='http://www.w3.org/1999/xhtml'>
<head>
  <meta charset='UTF-8'>
  <meta name='viewport' content='width=device-width, initial-scale=1.0'>
  <meta http-equiv='X-UA-Compatible' content='IE=edge'>
  <title>{{Heading}}</title>
  <!--[if mso]>
  <style type='text/css'>
    body, table, td, a { font-family: Arial, sans-serif !important; }
  </style>
  <![endif]-->
  <style type='text/css'>
    body { margin:0; padding:0; -webkit-text-size-adjust:100%; -ms-text-size-adjust:100%; }
    table, td { mso-table-lspace:0pt; mso-table-rspace:0pt; }
    img { -ms-interpolation-mode:bicubic; border:0; line-height:100%; outline:none; text-decoration:none; }
    @media (prefers-color-scheme: dark) {
      .email-bg { background-color: #0b0f19 !important; }
      .email-card { background-color: #111827 !important; border-color: #1f2937 !important; }
      .text-title { color: #f9fafb !important; }
      .text-body { color: #cbd5e1 !important; }
      .text-muted { color: #94a3b8 !important; }
      .settlement-card { background-color: #0f291e !important; border-color: #166534 !important; }
      .settlement-msg { color: #86efac !important; }
      .footer-text { color: #64748b !important; }
      .divider { border-color: #1f2937 !important; }
    }
    @media only screen and (max-width: 600px) {
      .container { width: 100% !important; padding: 12px !important; }
      .card-content { padding: 28px 20px !important; }
      .btn { display: block !important; width: 100% !important; text-align: center !important; }
    }
  </style>
</head>
<body class='email-bg' style='margin: 0; padding: 0; background-color: #f1f5f9; font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif; color: #334155;'>
  <!-- Preheader preview text -->
  <div style='display:none; font-size:1px; color:#f1f5f9; line-height:1px; max-height:0px; max-width:0px; opacity:0; overflow:hidden;'>
    {{MainMessage}}
  </div>

  <table role='presentation' border='0' cellpadding='0' cellspacing='0' width='100%' class='email-bg' style='background-color: #f1f5f9;'>
    <tr>
      <td align='center' style='padding: 40px 16px;'>
        <table role='presentation' class='container' border='0' cellpadding='0' cellspacing='0' width='100%' style='max-width: 540px;'>
          
          <!-- Logo & Header -->
          <tr>
            <td align='center' style='padding-bottom: 24px;'>
              <table role='presentation' border='0' cellpadding='0' cellspacing='0'>
                <tr>
                  <td align='center' style='vertical-align: middle;'>
                    <img src='https://splitx-exp.netlify.app/assets/donut-chart.png' alt='SplitX Logo' width='40' height='40' style='display: block; border-radius: 8px;' />
                  </td>
                  <td style='vertical-align: middle; padding-left: 10px;'>
                    <span style='font-size: 22px; font-weight: 800; color: #0f172a; letter-spacing: -0.5px; font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, sans-serif;'>
                      Split<span style='color: #7044ff;'>X</span>
                    </span>
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- Main Card -->
          <tr>
            <td>
              <table role='presentation' class='email-card' border='0' cellpadding='0' cellspacing='0' width='100%' style='background-color: #ffffff; border-radius: 16px; border: 1px solid #e2e8f0; box-shadow: 0 4px 16px rgba(15, 23, 42, 0.04); overflow: hidden;'>
                
                <!-- Decorative Top Border Accent -->
                <tr>
                  <td height='4' style='background: linear-gradient(90deg, #10b981 0%, #7044ff 100%); line-height: 4px; font-size: 4px;'>&nbsp;</td>
                </tr>

                <tr>
                  <td class='card-content' style='padding: 36px 36px 32px 36px;'>
                    
                    <!-- Status Icon Badge -->
                    <div style='text-align: center; margin-bottom: 20px;'>
                      <div style='display: inline-block; width: 56px; height: 56px; background-color: #ecfdf5; border-radius: 50%; line-height: 56px; text-align: center; font-size: 26px;'>
                        💸
                      </div>
                    </div>

                    <!-- Title -->
                    <h1 class='text-title' style='margin: 0 0 10px 0; font-size: 22px; font-weight: 700; color: #0f172a; text-align: center; letter-spacing: -0.3px;'>
                      {{Heading}}
                    </h1>

                    <!-- Greeting -->
                    <p class='text-body' style='margin: 0 0 20px 0; font-size: 15px; line-height: 1.6; color: #475569; text-align: center;'>
                      Hi <strong style='color: #0f172a;'>{{UserName}}</strong>,
                    </p>

                    <!-- Settlement Hero Callout Card -->
                    <div class='settlement-card' style='background-color: #f0fdf4; border: 1.5px solid #bbf7d0; border-radius: 12px; padding: 20px 24px; margin-bottom: 24px; text-align: center;'>
                      <span style='font-size: 12px; text-transform: uppercase; letter-spacing: 0.8px; color: #16a34a; font-weight: 700; display: block; margin-bottom: 6px;'>
                        TRANSACTION CONFIRMATION
                      </span>
                      <p class='settlement-msg' style='margin: 0; font-size: 16px; font-weight: 600; color: #15803d; line-height: 1.5;'>
                        {{MainMessage}}
                      </p>
                    </div>

                    <p class='text-body' style='margin: 0 0 28px 0; font-size: 14px; line-height: 1.6; color: #64748b; text-align: center;'>
                      Your group expense balance has been updated automatically. You can check the complete breakdown and history on your dashboard.
                    </p>

                    <!-- CTA Button -->
                    <table role='presentation' border='0' cellpadding='0' cellspacing='0' width='100%'>
                      <tr>
                        <td align='center'>
                          <a href='https://splitx-exp.netlify.app/tabs/expenses?roomId={{RoomId}}' class='btn' target='_blank' style='background-color: #7044ff; color: #ffffff !important; padding: 14px 34px; font-weight: 600; font-size: 15px; border-radius: 10px; display: inline-block; text-decoration: none; box-shadow: 0 4px 12px rgba(112, 68, 255, 0.28); letter-spacing: 0.2px;'>
                            View Dashboard &rarr;
                          </a>
                        </td>
                      </tr>
                    </table>

                    <!-- Divider -->
                    <hr class='divider' style='border: none; border-top: 1px solid #f1f5f9; margin: 28px 0 20px 0;' />

                    <!-- Note -->
                    <p class='text-muted' style='margin: 0; font-size: 13px; line-height: 1.5; color: #64748b; text-align: center;'>
                      Thank you for keeping your expenses organized with SplitX!
                    </p>

                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- Footer -->
          <tr>
            <td align='center' style='padding-top: 24px;'>
              <p class='footer-text' style='margin: 0 0 6px 0; font-size: 13px; color: #94a3b8; font-weight: 600;'>
                SplitX &bull; Effortless group expense splitting
              </p>
              <p class='footer-text' style='margin: 0; font-size: 12px; color: #94a3b8; line-height: 1.4;'>
                This is an automated notification from SplitX.
              </p>
            </td>
          </tr>

        </table>
      </td>
    </tr>
  </table>
</body>
</html>";

            var roomInvitationHtml = @"<!DOCTYPE html>
<html lang='en' xmlns='http://www.w3.org/1999/xhtml'>
<head>
  <meta charset='UTF-8'>
  <meta name='viewport' content='width=device-width, initial-scale=1.0'>
  <meta http-equiv='X-UA-Compatible' content='IE=edge'>
  <title>Invitation to join {{RoomName}}</title>
  <!--[if mso]>
  <style type='text/css'>
    body, table, td, a { font-family: Arial, sans-serif !important; }
  </style>
  <![endif]-->
  <style type='text/css'>
    body { margin:0; padding:0; -webkit-text-size-adjust:100%; -ms-text-size-adjust:100%; }
    table, td { mso-table-lspace:0pt; mso-table-rspace:0pt; }
    img { -ms-interpolation-mode:bicubic; border:0; line-height:100%; outline:none; text-decoration:none; }
    @media (prefers-color-scheme: dark) {
      .email-bg { background-color: #0b0f19 !important; }
      .email-card { background-color: #111827 !important; border-color: #1f2937 !important; }
      .text-title { color: #f9fafb !important; }
      .text-body { color: #cbd5e1 !important; }
      .text-muted { color: #94a3b8 !important; }
      .room-badge { background-color: #1e1b4b !important; border-color: #312e81 !important; }
      .room-name { color: #c7d2fe !important; }
      .feature-box { background-color: #1e293b !important; border-color: #334155 !important; }
      .feature-text { color: #cbd5e1 !important; }
      .link-box { background-color: #182234 !important; border-color: #334155 !important; color: #a5b4fc !important; }
      .footer-text { color: #64748b !important; }
      .divider { border-color: #1f2937 !important; }
    }
    @media only screen and (max-width: 600px) {
      .container { width: 100% !important; padding: 12px !important; }
      .card-content { padding: 28px 20px !important; }
      .btn { display: block !important; width: 100% !important; text-align: center !important; }
    }
  </style>
</head>
<body class='email-bg' style='margin: 0; padding: 0; background-color: #f1f5f9; font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif; color: #334155;'>
  <!-- Preheader text for inbox preview -->
  <div style='display:none; font-size:1px; color:#f1f5f9; line-height:1px; max-height:0px; max-width:0px; opacity:0; overflow:hidden;'>
    {{InviterName}} invited you to join the room '{{RoomName}}' on SplitX!
  </div>

  <table role='presentation' border='0' cellpadding='0' cellspacing='0' width='100%' class='email-bg' style='background-color: #f1f5f9;'>
    <tr>
      <td align='center' style='padding: 40px 16px;'>
        <table role='presentation' class='container' border='0' cellpadding='0' cellspacing='0' width='100%' style='max-width: 540px;'>
          
          <!-- Logo & Header -->
          <tr>
            <td align='center' style='padding-bottom: 24px;'>
              <table role='presentation' border='0' cellpadding='0' cellspacing='0'>
                <tr>
                  <td align='center' style='vertical-align: middle;'>
                    <img src='https://splitx-exp.netlify.app/assets/donut-chart.png' alt='SplitX Logo' width='40' height='40' style='display: block; border-radius: 8px;' />
                  </td>
                  <td style='vertical-align: middle; padding-left: 10px;'>
                    <span style='font-size: 22px; font-weight: 800; color: #0f172a; letter-spacing: -0.5px; font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, sans-serif;'>
                      Split<span style='color: #7044ff;'>X</span>
                    </span>
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- Main Card -->
          <tr>
            <td>
              <table role='presentation' class='email-card' border='0' cellpadding='0' cellspacing='0' width='100%' style='background-color: #ffffff; border-radius: 16px; border: 1px solid #e2e8f0; box-shadow: 0 4px 16px rgba(15, 23, 42, 0.04); overflow: hidden;'>
                
                <!-- Decorative Top Border Accent -->
                <tr>
                  <td height='4' style='background: linear-gradient(90deg, #7044ff 0%, #38bdf8 100%); line-height: 4px; font-size: 4px;'>&nbsp;</td>
                </tr>

                <tr>
                  <td class='card-content' style='padding: 36px 36px 32px 36px;'>
                    
                    <!-- Icon Badge -->
                    <div style='text-align: center; margin-bottom: 20px;'>
                      <div style='display: inline-block; width: 56px; height: 56px; background-color: #f3f0ff; border-radius: 50%; line-height: 56px; text-align: center; font-size: 26px;'>
                        👥
                      </div>
                    </div>

                    <!-- Title -->
                    <h1 class='text-title' style='margin: 0 0 10px 0; font-size: 22px; font-weight: 700; color: #0f172a; text-align: center; letter-spacing: -0.3px;'>
                      You're Invited to Join a Room!
                    </h1>

                    <!-- Greeting & Intro -->
                    <p class='text-body' style='margin: 0 0 8px 0; font-size: 15px; line-height: 1.6; color: #475569; text-align: center;'>
                      Hello <strong style='color: #0f172a;'>{{InviteeName}}</strong>,
                    </p>
                    <p class='text-body' style='margin: 0 0 24px 0; font-size: 15px; line-height: 1.6; color: #475569; text-align: center;'>
                      <strong style='color: #7044ff;'>{{InviterName}}</strong> has invited you to join their expense-sharing group on SplitX:
                    </p>

                    <!-- Room Spotlight Box -->
                    <table role='presentation' border='0' cellpadding='0' cellspacing='0' width='100%' style='margin-bottom: 24px;'>
                      <tr>
                        <td align='center'>
                          <div class='room-badge' style='display: inline-block; background-color: #faf5ff; border: 1.5px solid #e9d5ff; border-radius: 12px; padding: 14px 28px; text-align: center;'>
                            <span style='font-size: 13px; text-transform: uppercase; letter-spacing: 1px; color: #9333ea; font-weight: 700; display: block; margin-bottom: 4px;'>
                              GROUP ROOM
                            </span>
                            <span class='room-name' style='font-size: 20px; font-weight: 800; color: #581c87; letter-spacing: -0.3px;'>
                              {{RoomName}}
                            </span>
                          </div>
                        </td>
                      </tr>
                    </table>

                    <!-- Features Row -->
                    <div class='feature-box' style='background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 10px; padding: 14px 18px; margin-bottom: 28px;'>
                      <table role='presentation' border='0' cellpadding='0' cellspacing='0' width='100%'>
                        <tr>
                          <td class='feature-text' style='font-size: 13px; color: #475569; line-height: 1.6;'>
                            ⚡ <strong>Split expenses effortlessly</strong> &bull; Equal or custom splits<br/>
                            📊 <strong>Track balances in real time</strong> &bull; Total clarity for everyone<br/>
                            💳 <strong>Settle up easily</strong> &bull; Automatic net calculation
                          </td>
                        </tr>
                      </table>
                    </div>

                    <!-- CTA Button -->
                    <table role='presentation' border='0' cellpadding='0' cellspacing='0' width='100%'>
                      <tr>
                        <td align='center'>
                          <a href='{{JoinUrl}}' class='btn' target='_blank' style='background-color: #7044ff; color: #ffffff !important; padding: 14px 34px; font-weight: 600; font-size: 15px; border-radius: 10px; display: inline-block; text-decoration: none; box-shadow: 0 4px 12px rgba(112, 68, 255, 0.28); letter-spacing: 0.2px;'>
                            Accept Invitation & Join &rarr;
                          </a>
                        </td>
                      </tr>
                    </table>

                    <!-- Expiration Notice -->
                    <div style='text-align: center; margin-top: 22px;'>
                      <span style='display: inline-block; background-color: #f1f5f9; color: #64748b; font-size: 13px; font-weight: 600; padding: 5px 12px; border-radius: 20px;'>
                        ⏰ Link expires in 7 days
                      </span>
                    </div>

                    <!-- Fallback Link Section -->
                    <div class='info-box' style='margin-top: 26px; padding: 14px 16px; background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 10px;'>
                      <p class='text-muted' style='margin: 0 0 6px 0; font-size: 12px; color: #64748b; font-weight: 600;'>
                        Or copy and paste this link into your browser:
                      </p>
                      <p class='link-box' style='margin: 0; font-size: 12px; color: #7044ff; word-break: break-all; font-family: ui-monospace, Menlo, Consolas, monospace; line-height: 1.4;'>
                        {{JoinUrl}}
                      </p>
                    </div>

                    <!-- Divider -->
                    <hr class='divider' style='border: none; border-top: 1px solid #f1f5f9; margin: 28px 0 20px 0;' />

                    <!-- Disregard Note -->
                    <p class='text-muted' style='margin: 0; font-size: 13px; line-height: 1.5; color: #64748b; text-align: center;'>
                      If you weren't expecting this invitation, you can safely ignore this email.
                    </p>

                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- Footer -->
          <tr>
            <td align='center' style='padding-top: 24px;'>
              <p class='footer-text' style='margin: 0 0 6px 0; font-size: 13px; color: #94a3b8; font-weight: 600;'>
                SplitX &bull; Effortless group expense splitting
              </p>
              <p class='footer-text' style='margin: 0; font-size: 12px; color: #94a3b8; line-height: 1.4;'>
                This automated invitation was sent to you on behalf of {{InviterName}}.
              </p>
            </td>
          </tr>

        </table>
      </td>
    </tr>
  </table>
</body>
</html>";

            migrationBuilder.Sql(@"
UPDATE EmailTemplates
SET SubjectTemplate = 'Reset Your Password - SplitX',
    BodyHtml = '" + passwordResetHtml.Replace("'", "''") + @"',
    UpdatedDate = DATEADD(MINUTE, 330, GETUTCDATE())
WHERE TemplateKey = 'PasswordReset';

UPDATE EmailTemplates
SET SubjectTemplate = '{{Heading}} - {{SettlementMonth}}',
    BodyHtml = '" + settlementHtml.Replace("'", "''") + @"',
    UpdatedDate = DATEADD(MINUTE, 330, GETUTCDATE())
WHERE TemplateKey = 'SettlementNotification';

UPDATE EmailTemplates
SET SubjectTemplate = 'You''re invited to join ''{{RoomName}}'' on SplitX',
    BodyHtml = '" + roomInvitationHtml.Replace("'", "''") + @"',
    UpdatedDate = DATEADD(MINUTE, 330, GETUTCDATE())
WHERE TemplateKey = 'RoomInvitation';
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
