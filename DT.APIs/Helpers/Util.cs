
using System.Drawing;
using System.Drawing.Imaging;




namespace DT.APIs.Helpers
{
    public static class Util
    {
        private static IHttpContextAccessor _httpContextAccessor;
        private static IConfiguration _configuration;
        private static string _encryptionKey = "";
        public static void Configure(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;

        }
        public static string ExtractUsername(string email)
        {
            // Check if the email ends with "@stc.com.sa"
            string domain = "@stc.com.sa";
            if (email.EndsWith(domain))
            {
                // Remove the domain part first
                string localPart = email.Substring(0, email.Length - domain.Length);

                // Check if the local part ends with ".y" (single character after dot)
                if (localPart.Length > 2 && localPart[localPart.Length - 2] == '.' && localPart[localPart.Length - 1].ToString().Length == 1)
                {
                    // Remove the last two characters (".y")
                    localPart = localPart.Substring(0, localPart.Length - 2);
                }

                // Return the local part in lowercase and append the domain
                return localPart.ToLower();
            }

            // Return the email as is if it doesn't match the expected domain
            return email.ToLower();
        }

        public static string GenerateAvatarText(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            string[] _sFullName = text.Split(' ');
            List<string> _sResult = new List<string>();
            string _result = "";


            for (int i = 0; i <= _sFullName.Length - 1; i++)
            {
                if (_sFullName[i].Length >= 3)
                {
                    _sResult.Add(_sFullName[i]);
                }
            }

            if (_sResult.Count == 1)
            {
                _result = _sResult[0].Substring(0, 1);
            }
            else if (_sResult.Count > 1)
            {
                _result = _sResult[0].Substring(0, 1) + _sResult[_sResult.Count - 1].Substring(0, 1);
            }
            else
            {
                _result = "?";
            }
            return _result.ToUpper();
        }

        public static byte[] GenerateAvatarImage(string text, string color = "#7d8c75")
        {
            // Define colors
            Color fontColor = ColorTranslator.FromHtml("#FFF");
            Color bgColor = ColorTranslator.FromHtml(color);
            Font font = new Font("Arial", 45, FontStyle.Regular);

            // Create a dummy image to measure text size
            using (Image img = new Bitmap(1, 1))
            using (Graphics drawing = Graphics.FromImage(img))
            {
                SizeF textSize = drawing.MeasureString(text, font);

                // Create the final image
                using (Bitmap finalImg = new Bitmap(110, 110))
                using (Graphics finalDrawing = Graphics.FromImage(finalImg))
                {
                    // Clear background
                    finalDrawing.Clear(bgColor);

                    // Set up text drawing
                    StringFormat stringFormat = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        FormatFlags = StringFormatFlags.LineLimit,
                        Trimming = StringTrimming.Character
                    };

                    // Draw the text
                    finalDrawing.DrawString(text, font, new SolidBrush(fontColor), new Rectangle(0, 20, 110, 110), stringFormat);

                    // Save to memory stream
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        finalImg.Save(memoryStream, ImageFormat.Jpeg);
                        return memoryStream.ToArray(); // Return byte array
                    }
                }
            }
        }










    }
}
