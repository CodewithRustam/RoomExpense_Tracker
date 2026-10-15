using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailTemplatesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailTemplates",
                columns: table => new
                {
                    EmailTemplateId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SubjectTemplate = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    BodyHtml = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "DATEADD(MINUTE, 330, GETUTCDATE())"),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailTemplates", x => x.EmailTemplateId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_TemplateKey",
                table: "EmailTemplates",
                column: "TemplateKey",
                unique: true);

            var passwordResetHtml = @"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8' />
    <meta name='viewport' content='width=device-width, initial-scale=1.0' />
    <title>Reset Your Password</title>
    <!-- Fallback for email clients that don't support CSS -->
    <style type='text/css'>
        a { color: #7044ff; text-decoration: none; }
        .button { 
            background-color: #7044ff; 
            color: white; 
            padding: 12px 28px; 
            font-weight: 600; 
            border-radius: 6px; 
            display: inline-block; 
            font-size: 16px; 
            text-align: center;
            transition: background-color 0.2s;
        }
        .button:hover { background-color: #5d3ad4; }
        @media (prefers-color-scheme: dark) {
            .email-body { background-color: #1a1a1a; color: #e0e0e0; }
            .card { background-color: #2d2d2d; border-color: #444; }
            .text-muted { color: #aaaaaa !important; }
            hr { border-color: #444 !important; }
        }
        @media only screen and (max-width: 480px) {
            .container { width: 100% !important; padding: 16px !important; }
            .button { font-size: 15px !important; padding: 11px 24px !important; }
        }
    </style>
</head>
<body class='email-body' style='margin:0; padding:0; background-color:#f9f9fb; font-family: -apple-system, BlinkMacSystemFont, ''Segoe UI'', Roboto, Helvetica, Arial, sans-serif; color:#333333;'>
    <table role='presentation' border='0' cellpadding='0' cellspacing='0' width='100%' style='background-color:#f9f9fb;'>
        <tr>
            <td align='center' style='padding: 20px 0;'>
                <!-- Main Container -->
                <table role='presentation' class='container' border='0' cellpadding='0' cellspacing='0' width='100%' style='max-width: 600px;'>
                    <tr>
                        <td align='center'>
                            <!-- Card -->
                            <table role='presentation' class='card' border='0' cellpadding='0' cellspacing='0' width='100%' style='background-color:#ffffff; border-radius:12px; overflow:hidden; box-shadow:0 4px 12px rgba(0,0,0,0.05); border:1px solid #e5e7eb; margin-bottom:24px;'>
                                <tr>
                                    <td style='padding:32px 40px; text-align:center;'>
                                        <!-- Logo / Brand (Optional: Replace with your logo URL) -->
                                         <img src='https://splitx-exp.netlify.app/assets/donut-chart.png' alt='Your App' width='48' style='margin-bottom:16px;' /> 
                                        
                                        <h1 style='margin:0 0 16px 0; font-size:24px; font-weight:700; color:#111111;'>
                                            Password Reset Request
                                        </h1>
                                        
                                        <p style='margin:0 0 20px 0; font-size:16px; line-height:1.5; color:#555555;'>
                                            Hello <strong>{{MemberName}}</strong>,
                                        </p>
                                        
                                        <p style='margin:0 0 24px 0; font-size:15px; line-height:1.6; color:#666666;'>
                                            We received a request to reset the password for your account. 
                                            Click the button below to create a new secure password.
                                        </p>

                                        <!-- CTA Button -->
                                        <div style='margin:28px 0; text-align:center;'>
                                            <a href='{{ResetLink}}' 
                                               class='button' 
                                               target='_blank'
                                               style='background-color:#7044ff; color:#ffffff; padding:12px 28px; font-weight:600; border-radius:6px; display:inline-block; font-size:16px; text-decoration:none; box-shadow:0 2px 4px rgba(112,68,255,0.2);'>
                                                Reset Password
                                            </a>
                                        </div>

                                        <p style='margin:24px 0 0 0; font-size:14px; line-height:1.6; color:#888888;'>
                                            This link will expire in <strong>1 hour</strong> for security.
                                        </p>

                                        <hr style='border:none; border-top:1px solid #e5e7eb; margin:32px 0;' />

                                        <p class='text-muted' style='margin:0; font-size:13px; line-height:1.5; color:#999999;'>
                                            If you didn’t request this, you can safely ignore this email. 
                                            Your account remains secure and no changes have been made.
                                        </p>
                                    </td>
                                </tr>
                            </table>

                            <!-- Footer -->
                            <table role='presentation' border='0' cellpadding='0' cellspacing='0' width='100%'>
                                <tr>
                                    <td align='center' style='padding:0 20px;'>
                                        <p style='margin:0; font-size:12px; color:#aaaaaa; line-height:1.4;'>
                                            This is an automated message — please do not reply.
                                        </p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

            var settlementHtml = @"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8' />
    <meta name='viewport' content='width=device-width, initial-scale=1.0' />
    <title>{{Heading}}</title>
    <style type='text/css'>
        body { margin:0; padding:0; background-color:#f9f9fb; font-family: -apple-system, BlinkMacSystemFont, ''Segoe UI'', Roboto, Helvetica, Arial, sans-serif; }
        a { color: #7044ff; text-decoration: none; }
        .button {
            background-color: #7044ff;
            color: white !important;
            padding: 12px 28px;
            font-weight: 600;
            border-radius: 6px;
            display: inline-block;
            font-size: 16px;
            text-align: center;
            text-decoration: none;
            box-shadow: 0 2px 4px rgba(112,68,255,0.2);
        }
        .button:hover { background-color: #5d3ad4; }
        @media (prefers-color-scheme: dark) {
            .email-body { background-color: #1a1a1a; }
            .card { background-color: #2d2d2d !important; border-color: #444 !important; }
            .text-muted { color: #aaaaaa !important; }
            .footer { color: #888888 !important; }
            hr { border-color: #444 !important; }
        }
        @media only screen and (max-width: 480px) {
            .container { width: 100% !important; padding: 16px !important; }
            .button { font-size: 15px !important; padding: 11px 24px !important; }
        }
    </style>
</head>
<body class='email-body' style='margin:0; padding:0; background-color:#f9f9fb;'>
    <!-- Preheader (hidden preview text) -->
    <div style='display:none; font-size:1px; color:#f9f9fb; line-height:1px; max-height:0px; max-width:0px; opacity:0; overflow:hidden;'>
        {{MainMessage}}
    </div>

    <table role='presentation' border='0' cellpadding='0' cellspacing='0' width='100%' style='background-color:#f9f9fb;'>
        <tr>
            <td align='center' style='padding: 20px 0;'>
                <!-- Main Container -->
                <table role='presentation' class='container' border='0' cellpadding='0' cellspacing='0' width='100%' style='max-width: 600px;'>
                    <tr>
                        <td align='center'>
                            <!-- Card -->
                            <table role='presentation' class='card' border='0' cellpadding='0' cellspacing='0' width='100%' style='background-color:#ffffff; border-radius:12px; overflow:hidden; box-shadow:0 4px 12px rgba(0,0,0,0.05); border:1px solid #e5e7eb; margin-bottom:24px;'>
                                <tr>
                                    <td style='padding:32px 40px; text-align:center;'>
                                        <!-- Optional Logo -->
                                         <img src='https://splitx-exp.netlify.app/assets/donut-chart.png' alt='Expense Tracker' width='48' style='margin-bottom:16px;' />

                                        <h1 style='margin:0 0 16px 0; font-size:24px; font-weight:700; color:#111111; line-height:1.2;'>
                                            {{Heading}}
                                        </h1>

                                        <p style='margin:0 0 20px 0; font-size:16px; line-height:1.6; color:#555555;'>
                                            Hi <strong>{{UserName}}</strong>,
                                        </p>

                                        <p style='margin:0 0 28px 0; font-size:15px; line-height:1.7; color:#666666;'>
                                            {{MainMessage}}
                                        </p>

                                        <!-- Optional CTA (e.g., View Dashboard) -->
                                        <div style='margin:32px 0; text-align:center;'>
                                            <a href='https://splitx-exp.netlify.app/tabs/expenses?roomId={{RoomId}}' 
                                               class='button' 
                                               target='_blank'
                                               style='background-color:#7044ff; color:#ffffff; padding:12px 28px; font-weight:600; border-radius:6px; display:inline-block; font-size:16px; text-decoration:none; box-shadow:0 2px 4px rgba(112,68,255,0.2);'>
                                                View Dashboard
                                            </a>
                                        </div>

                                        <p style='margin:24px 0 0 0; font-size:14px; color:#888888;'>
                                            Thank you for keeping your expenses organized!
                                        </p>

                                        <hr style='border:none; border-top:1px solid #e5e7eb; margin:32px 0;' />

                                        <p class='text-muted' style='margin:0; font-size:13px; line-height:1.5; color:#999999;'>
                                            This is an automated notification from SplitX.
                                        </p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

            var roomInvitationHtml = @"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8' />
    <meta name='viewport' content='width=device-width, initial-scale=1.0' />
    <title>Invitation to join {{RoomName}}</title>
    <style type='text/css'>
        body { margin:0; padding:0; background-color:#f9f9fb; font-family: -apple-system, BlinkMacSystemFont, ''Segoe UI'', Roboto, Helvetica, Arial, sans-serif; }
        a { color: #7044ff; text-decoration: none; }
        .button {
            background-color: #7044ff;
            color: #ffffff !important;
            padding: 13px 32px;
            font-weight: 600;
            border-radius: 8px;
            display: inline-block;
            font-size: 16px;
            text-align: center;
            text-decoration: none;
            box-shadow: 0 4px 10px rgba(112,68,255,0.25);
            transition: background-color 0.2s;
        }
        .button:hover { background-color: #5d3ad4; }
        @media (prefers-color-scheme: dark) {
            .email-body { background-color: #1a1a1a !important; }
            .card { background-color: #2d2d2d !important; border-color: #444 !important; }
            .title-text { color: #ffffff !important; }
            .sub-text { color: #cccccc !important; }
            .room-chip { background-color: #383838 !important; border-color: #555 !important; color: #ffffff !important; }
            .text-muted { color: #aaaaaa !important; }
            hr { border-color: #444 !important; }
        }
        @media only screen and (max-width: 480px) {
            .container { width: 100% !important; padding: 16px !important; }
            .button { font-size: 15px !important; padding: 12px 26px !important; display: block !important; }
        }
    </style>
</head>
<body class='email-body' style='margin:0; padding:0; background-color:#f9f9fb;'>
    <!-- Preheader preview text -->
    <div style='display:none; font-size:1px; color:#f9f9fb; line-height:1px; max-height:0px; max-width:0px; opacity:0; overflow:hidden;'>
        {{InviterName}} has invited you to join the room '{{RoomName}}' on SplitX!
    </div>

    <table role='presentation' border='0' cellpadding='0' cellspacing='0' width='100%' style='background-color:#f9f9fb;'>
        <tr>
            <td align='center' style='padding: 24px 0;'>
                <table role='presentation' class='container' border='0' cellpadding='0' cellspacing='0' width='100%' style='max-width: 600px;'>
                    <tr>
                        <td align='center'>
                            <!-- Card -->
                            <table role='presentation' class='card' border='0' cellpadding='0' cellspacing='0' width='100%' style='background-color:#ffffff; border-radius:14px; overflow:hidden; box-shadow:0 6px 18px rgba(0,0,0,0.06); border:1px solid #e5e7eb; margin-bottom:24px;'>
                                <tr>
                                    <td style='padding:36px 40px; text-align:center;'>
                                        <!-- Logo -->
                                        <img src='https://splitx-exp.netlify.app/assets/donut-chart.png' alt='SplitX' width='52' height='52' style='margin-bottom:20px; display:inline-block;' />

                                        <h1 class='title-text' style='margin:0 0 12px 0; font-size:24px; font-weight:700; color:#111111; line-height:1.3;'>
                                            You're Invited to Join a Room!
                                        </h1>

                                        <p class='sub-text' style='margin:0 0 20px 0; font-size:16px; line-height:1.6; color:#555555;'>
                                            Hello <strong>{{InviteeName}}</strong>,
                                        </p>

                                        <p class='sub-text' style='margin:0 0 24px 0; font-size:15px; line-height:1.7; color:#666666;'>
                                            <strong>{{InviterName}}</strong> has invited you to join their group expense room on SplitX:
                                        </p>

                                        <!-- Room Highlight Chip -->
                                        <div style='margin:0 auto 28px auto; display:inline-block;'>
                                            <div class='room-chip' style='background-color:#f3f0ff; border:1px solid #dcd1ff; border-radius:10px; padding:12px 24px; font-size:18px; font-weight:700; color:#7044ff; letter-spacing:0.3px;'>
                                                👥 {{RoomName}}
                                            </div>
                                        </div>

                                        <p class='sub-text' style='margin:0 0 28px 0; font-size:14px; line-height:1.6; color:#777777;'>
                                            Collaborate effortlessly, log shared bills, and keep balances transparent and settled.
                                        </p>

                                        <!-- CTA Button -->
                                        <div style='margin:28px 0; text-align:center;'>
                                            <a href='{{JoinUrl}}' 
                                               class='button' 
                                               target='_blank'
                                               style='background-color:#7044ff; color:#ffffff !important; padding:13px 32px; font-weight:600; border-radius:8px; display:inline-block; font-size:16px; text-decoration:none; box-shadow:0 4px 10px rgba(112,68,255,0.25);'>
                                                Accept Invitation & Join
                                            </a>
                                        </div>

                                        <p style='margin:24px 0 0 0; font-size:13px; color:#888888;'>
                                            ⏰ This invitation link is valid for <strong>7 days</strong>.
                                        </p>

                                        <hr style='border:none; border-top:1px solid #e5e7eb; margin:32px 0;' />

                                        <p class='text-muted' style='margin:0; font-size:13px; line-height:1.5; color:#999999;'>
                                            If you don't know the person who invited you, you can safely disregard this email.
                                        </p>
                                    </td>
                                </tr>
                            </table>

                            <!-- Footer -->
                            <table role='presentation' border='0' cellpadding='0' cellspacing='0' width='100%'>
                                <tr>
                                    <td align='center' style='padding:0 20px;'>
                                        <p style='margin:0; font-size:12px; color:#aaaaaa; line-height:1.4;'>
                                            SplitX &bull; Effortless group expense splitting
                                        </p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

            migrationBuilder.Sql($@"
IF NOT EXISTS (SELECT 1 FROM EmailTemplates WHERE TemplateKey = 'PasswordReset')
BEGIN
    INSERT INTO EmailTemplates (TemplateKey, SubjectTemplate, BodyHtml, Description, IsActive, CreatedDate)
    VALUES (
        'PasswordReset',
        'Reset Your Password',
        '{passwordResetHtml.Replace("'", "''")}',
        'Password reset request template sent to users who requested a password reset',
        1,
        DATEADD(MINUTE, 330, GETUTCDATE())
    );
END

IF NOT EXISTS (SELECT 1 FROM EmailTemplates WHERE TemplateKey = 'SettlementNotification')
BEGIN
    INSERT INTO EmailTemplates (TemplateKey, SubjectTemplate, BodyHtml, Description, IsActive, CreatedDate)
    VALUES (
        'SettlementNotification',
        '{{{{Heading}}}} - {{{{SettlementMonth}}}}',
        '{settlementHtml.Replace("'", "''")}',
        'Settlement notification template sent to payer and receiver on settlement recording',
        1,
        DATEADD(MINUTE, 330, GETUTCDATE())
    );
END

IF NOT EXISTS (SELECT 1 FROM EmailTemplates WHERE TemplateKey = 'RoomInvitation')
BEGIN
    INSERT INTO EmailTemplates (TemplateKey, SubjectTemplate, BodyHtml, Description, IsActive, CreatedDate)
    VALUES (
        'RoomInvitation',
        'You''re invited to join ''{{{{RoomName}}}}'' on SplitX',
        '{roomInvitationHtml.Replace("'", "''")}',
        'Room invitation template sent to invitees when a room is created or a member is added',
        1,
        DATEADD(MINUTE, 330, GETUTCDATE())
    );
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailTemplates");
        }
    }
}
