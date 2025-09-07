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
    }
}
