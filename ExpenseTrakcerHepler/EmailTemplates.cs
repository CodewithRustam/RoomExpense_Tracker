namespace ExpenseTrakcerHepler
{
    public static class EmailTemplates
    {
        public static string GetPasswordResetEmail(string resetLink, string memberName)
        {
            const string ionicTertiary = "#7044ff"; // Ionic Tertiary Color
            const string ionicTertiaryHover = "#5d3ad4"; // Darker shade for hover

            return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8' />
    <meta name='viewport' content='width=device-width, initial-scale=1.0' />
    <title>Reset Your Password</title>
    <!-- Fallback for email clients that don't support CSS -->
    <style type='text/css'>
        a {{ color: {ionicTertiary}; text-decoration: none; }}
        .button {{ 
            background-color: {ionicTertiary}; 
            color: white; 
            padding: 12px 28px; 
            font-weight: 600; 
            border-radius: 6px; 
            display: inline-block; 
            font-size: 16px; 
            text-align: center;
            transition: background-color 0.2s;
        }}
        .button:hover {{ background-color: {ionicTertiaryHover}; }}
        @media (prefers-color-scheme: dark) {{
            .email-body {{ background-color: #1a1a1a; color: #e0e0e0; }}
            .card {{ background-color: #2d2d2d; border-color: #444; }}
            .text-muted {{ color: #aaaaaa !important; }}
            hr {{ border-color: #444 !important; }}
        }}
        @media only screen and (max-width: 480px) {{
            .container {{ width: 100% !important; padding: 16px !important; }}
            .button {{ font-size: 15px !important; padding: 11px 24px !important; }}
        }}
    </style>
</head>
<body class='email-body' style='margin:0; padding:0; background-color:#f9f9fb; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; color:#333333;'>
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
                                            Hello <strong>{memberName}</strong>,
                                        </p>
                                        
                                        <p style='margin:0 0 24px 0; font-size:15px; line-height:1.6; color:#666666;'>
                                            We received a request to reset the password for your account. 
                                            Click the button below to create a new secure password.
                                        </p>

                                        <!-- CTA Button -->
                                        <div style='margin:28px 0; text-align:center;'>
                                            <a href='{resetLink}' 
                                               class='button' 
                                               target='_blank'
                                               style='background-color:{ionicTertiary}; color:#ffffff; padding:12px 28px; font-weight:600; border-radius:6px; display:inline-block; font-size:16px; text-decoration:none; box-shadow:0 2px 4px rgba(112,68,255,0.2);'>
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
</html>
";
        }        /// <summary>
                 /// Returns a clean, professional HTML email template
                 /// </summary>
        public static string GetSettlementEmailTemplate(string userName, string mainMessage, string heading,int roomId)
        {
            const string ionicTertiary = "#7044ff";
            const string ionicTertiaryHover = "#5d3ad4";

            return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8' />
    <meta name='viewport' content='width=device-width, initial-scale=1.0' />
    <title>{heading}</title>
    <style type='text/css'>
        body {{ margin:0; padding:0; background-color:#f9f9fb; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; }}
        a {{ color: {ionicTertiary}; text-decoration: none; }}
        .button {{
            background-color: {ionicTertiary};
            color: white !important;
            padding: 12px 28px;
            font-weight: 600;
            border-radius: 6px;
            display: inline-block;
            font-size: 16px;
            text-align: center;
            text-decoration: none;
            box-shadow: 0 2px 4px rgba(112,68,255,0.2);
        }}
        .button:hover {{ background-color: {ionicTertiaryHover}; }}
        @media (prefers-color-scheme: dark) {{
            .email-body {{ background-color: #1a1a1a; }}
            .card {{ background-color: #2d2d2d !important; border-color: #444 !important; }}
            .text-muted {{ color: #aaaaaa !important; }}
            .footer {{ color: #888888 !important; }}
            hr {{ border-color: #444 !important; }}
        }}
        @media only screen and (max-width: 480px) {{
            .container {{ width: 100% !important; padding: 16px !important; }}
            .button {{ font-size: 15px !important; padding: 11px 24px !important; }}
        }}
    </style>
</head>
<body class='email-body' style='margin:0; padding:0; background-color:#f9f9fb;'>
    <!-- Preheader (hidden preview text) -->
    <div style='display:none; font-size:1px; color:#f9f9fb; line-height:1px; max-height:0px; max-width:0px; opacity:0; overflow:hidden;'>
        {mainMessage.Replace("<br>", " ")}
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
                                         <img src='https://splitx-exp.netlify.app/assets/donut-chart.png
' alt='Expense Tracker' width='48' style='margin-bottom:16px;' />

                                        <h1 style='margin:0 0 16px 0; font-size:24px; font-weight:700; color:#111111; line-height:1.2;'>
                                            {heading}
                                        </h1>

                                        <p style='margin:0 0 20px 0; font-size:16px; line-height:1.6; color:#555555;'>
                                            Hi <strong>{userName}</strong>,
                                        </p>

                                        <p style='margin:0 0 28px 0; font-size:15px; line-height:1.7; color:#666666;'>
                                            {mainMessage}
                                        </p>

                                        <!-- Optional CTA (e.g., View Dashboard) -->
                                        <div style='margin:32px 0; text-align:center;'>
                                            <a href='https://splitx-exp.netlify.app/tabs/expenses?roomId={roomId}' 
                                               class='button' 
                                               target='_blank'
                                               style='background-color:{ionicTertiary}; color:#ffffff; padding:12px 28px; font-weight:600; border-radius:6px; display:inline-block; font-size:16px; text-decoration:none; box-shadow:0 2px 4px rgba(112,68,255,0.2);'>
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
</html>
";
        }
    }
}
