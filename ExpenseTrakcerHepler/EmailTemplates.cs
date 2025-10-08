namespace ExpenseTrakcerHepler
{
    public static class EmailTemplates
    {
        public static string GetPasswordResetEmail(string resetLink)
        {
            return $@"
        <div style='font-family: Arial, sans-serif; color:#333;'>
            <h2 style='color:#16a34a;'>Password Reset Request</h2>
            <p>Hello,</p>
            <p>We received a request to reset your password for your account. 
               Click the button below to set a new password:</p>
            
            <p style='margin:20px 0;'>
                <a href='{resetLink}' 
                   style='background-color:#16a34a; color:white; padding:10px 20px; 
                          text-decoration:none; border-radius:5px;'>
                    Reset Password
                </a>
            </p>

            <p>If the button doesn’t work, copy and paste the following link into your browser:</p>
            <p><a href='{resetLink}'>{resetLink}</a></p>

            <p style='margin-top:20px; font-size:12px; color:#666;'>
                If you did not request a password reset, please ignore this email. 
                Your account will remain secure.
            </p>
            <hr style='border:none; border-top:1px solid #ddd; margin:20px 0;' />
            <p style='font-size:12px; color:#888;'>This is an automated message. Please do not reply.</p>
        </div>
        ";
        }
        /// <summary>
        /// Returns a clean, professional HTML email template
        /// </summary>
        public static string GetEmailTemplate(string userName, string mainMessage, string heading)
        {
            return $@"
                    <!DOCTYPE html>
                    <html lang='en'>
                    <head>
                        <meta charset='UTF-8'>
                        <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                        <style>
                            body {{
                                font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                                background-color: #f4f4f7;
                                color: #333;
                                margin: 0;
                                padding: 0;
                            }}
                            .email-container {{
                                max-width: 600px;
                                margin: 30px auto;
                                background-color: #ffffff;
                                border-radius: 10px;
                                padding: 20px;
                                box-shadow: 0 4px 12px rgba(0,0,0,0.05);
                            }}
                            h2 {{
                                color: #4e54c8;
                                text-align: center;
                            }}
                            p {{
                                line-height: 1.6;
                                font-size: 16px;
                            }}
                            .footer {{
                                font-size: 14px;
                                color: #777;
                                text-align: center;
                                margin-top: 20px;
                            }}
                            .button {{
                                display: inline-block;
                                padding: 10px 20px;
                                margin-top: 20px;
                                background: #4e54c8;
                                color: #fff;
                                text-decoration: none;
                                border-radius: 5px;
                            }}
                        </style>
                    </head>
                    <body>
                        <div class='email-container'>
                            <h2>{heading}</h2>
                            <p>Hi {userName},</p>
                            <p>{mainMessage}</p>
                            <p>Thank you for using Expense Tracker!</p>
                            <div class='footer'>
                                &copy; {DateTime.UtcNow.Year} Expense Tracker. All rights reserved.
                            </div>
                        </div>
                    </body>
                    </html>";
        }
    }
}
